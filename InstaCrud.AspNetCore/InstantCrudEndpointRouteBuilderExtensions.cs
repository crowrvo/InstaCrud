using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using InstaCrud.Core;
using InstaCrud.Core.Querying;
using InstaCrud.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace InstaCrud.AspNetCore;

public static class InstantCrudEndpointRouteBuilderExtensions {
    private static readonly MethodInfo MapEntityMethod = typeof(InstantCrudEndpointRouteBuilderExtensions)
        .GetMethod(nameof(MapEntity), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static IEndpointRouteBuilder MapInstantCrud(this IEndpointRouteBuilder endpoints) {
        ArgumentNullException.ThrowIfNull(endpoints);

        InstantCrudOptions options = endpoints.ServiceProvider
            .GetRequiredService<InstantCrudOptions>();
        IEntityRegistry registry = endpoints.ServiceProvider
            .GetRequiredService<IEntityRegistry>();

        if (options.MapDocumentation) {
            endpoints.MapOpenApi(options.OpenApiPattern);
            endpoints.MapScalarApiReference(options.ScalarPath, scalar => scalar
                .WithTitle(options.ApiTitle)
                .WithOpenApiRoutePattern(options.OpenApiPattern)
                .DisableAgent());
        }

        RouteGroupBuilder api = endpoints.MapGroup(options.RoutePrefix);

        foreach (CrudEntityDefinition entity in registry.Entities) {
            MapEntityMethod
                .MakeGenericMethod(entity.EntityType)
                .Invoke(null, [api, entity]);
        }

        return endpoints;
    }

    private static void MapEntity<TEntity>(
        RouteGroupBuilder api,
        CrudEntityDefinition entity)
        where TEntity : class {
        RouteGroupBuilder group = api
            .MapGroup($"/{entity.RouteName}")
            .WithTags(entity.RouteName);
        string operationPrefix = $"InstantCrud_{entity.RouteName}";

        group.MapGet("", (ICrudExecutor executor, int? page, int? pageSize, CancellationToken cancellationToken) =>
                ListAsync<TEntity>(executor, entity, page, pageSize, cancellationToken))
            .WithName($"{operationPrefix}_List")
            .Produces<IReadOnlyList<TEntity>>()
            .Produces<PagedResult<TEntity>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("", InsertAsync<TEntity>)
            .WithName($"{operationPrefix}_Insert")
            .Produces<TEntity>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        if (!entity.HasKey)
            return;

        string keyRoute = string.Concat(entity.KeyProperties.Select(property =>
            $"/{{{JsonPropertyName(property)}}}"));

        group.MapGet(keyRoute, (HttpContext context, ICrudExecutor executor, CancellationToken cancellationToken) =>
                FindAsync<TEntity>(context, executor, entity, cancellationToken))
            .WithName($"{operationPrefix}_Find")
            .Produces<TEntity>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut(keyRoute, (HttpContext context, TEntity value, ICrudExecutor executor, CancellationToken cancellationToken) =>
                UpdateAsync(context, value, executor, entity, cancellationToken))
            .WithName($"{operationPrefix}_Update")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapMethods(keyRoute, [HttpMethods.Patch], (HttpContext context, ICrudExecutor executor, CancellationToken cancellationToken) =>
                PatchAsync<TEntity>(context, executor, entity, cancellationToken))
            .WithName($"{operationPrefix}_Patch")
            .Accepts<TEntity>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete(keyRoute, (HttpContext context, ICrudExecutor executor, CancellationToken cancellationToken) =>
                DeleteAsync<TEntity>(context, executor, entity, cancellationToken))
            .WithName($"{operationPrefix}_Delete")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> ListAsync<TEntity>(
        ICrudExecutor executor,
        CrudEntityDefinition entity,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
        where TEntity : class {
        if (page is null && pageSize is null) {
            IReadOnlyList<TEntity> entities = await executor
                .SelectAsync<TEntity>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return Results.Ok(entities);
        }

        if (page is null || pageSize is null) {
            return Results.Problem(
                "Os parâmetros page e pageSize precisam ser informados juntos.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try {
            CrudPropertyDefinition orderProperty = entity.KeyProperties.FirstOrDefault() ??
                entity.Properties.First(property => !property.Ignore);
            var query = new CrudQuery<TEntity>()
                .OrderBy(orderProperty.PropertyName)
                .Page(page.Value, pageSize.Value);
            PagedResult<TEntity> result = await executor
                .PageAsync(query, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return Results.Ok(result);
        }
        catch (ArgumentOutOfRangeException exception) {
            return Results.Problem(
                exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> InsertAsync<TEntity>(
        HttpContext context,
        TEntity value,
        ICrudExecutor executor,
        CancellationToken cancellationToken)
        where TEntity : class {
        await executor
            .InsertAsync(value, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return Results.Created(context.Request.Path, value);
    }

    private static async Task<IResult> FindAsync<TEntity>(
        HttpContext context,
        ICrudExecutor executor,
        CrudEntityDefinition entity,
        CancellationToken cancellationToken)
        where TEntity : class {
        if (!TryReadKeys(context, entity, out Dictionary<string, object?> keys, out string? error))
            return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);

        TEntity? value = await executor
            .FindAsync<TEntity>(keys, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return value is null ? Results.NotFound() : Results.Ok(value);
    }

    private static async Task<IResult> UpdateAsync<TEntity>(
        HttpContext context,
        TEntity value,
        ICrudExecutor executor,
        CrudEntityDefinition entity,
        CancellationToken cancellationToken)
        where TEntity : class {
        if (!TryAssignKeys(context, value, entity, out string? error))
            return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);

        int affectedRows = await executor
            .UpdateAsync(value, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return affectedRows == 0 ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<IResult> PatchAsync<TEntity>(
        HttpContext context,
        ICrudExecutor executor,
        CrudEntityDefinition entity,
        CancellationToken cancellationToken)
        where TEntity : class {
        JsonDocument document;

        try {
            document = await JsonDocument
                .ParseAsync(context.Request.Body, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException exception) {
            return Results.Problem(
                exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        using (document) {
            if (document.RootElement.ValueKind != JsonValueKind.Object) {
                return Results.Problem(
                    "O corpo do patch precisa ser um objeto JSON.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            TEntity? value;

            try {
                value = Activator.CreateInstance<TEntity>();
            }
            catch (MissingMethodException) {
                return Results.Problem(
                    $"A entidade '{typeof(TEntity).Name}' precisa de um construtor sem parâmetros para receber PATCH.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (value is null) {
                return Results.Problem(
                    $"Não foi possível criar a entidade '{typeof(TEntity).Name}' para receber PATCH.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (!TryAssignKeys(context, value, entity, out string? error))
                return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);

            var propertyNames = new List<string>();

            try {
                foreach (JsonProperty jsonProperty in document.RootElement.EnumerateObject()) {
                    CrudPropertyDefinition? property = FindProperty(entity, jsonProperty.Name);

                    if (property is null) {
                        return Results.Problem(
                            $"A propriedade JSON '{jsonProperty.Name}' não pertence à entidade '{typeof(TEntity).Name}'.",
                            statusCode: StatusCodes.Status400BadRequest);
                    }

                    if (property.IsKey)
                        continue;

                    if (property.Setter is null) {
                        return Results.Problem(
                            $"A propriedade '{property.PropertyName}' precisa ser gravável para receber PATCH.",
                            statusCode: StatusCodes.Status400BadRequest);
                    }

                    object? propertyValue = jsonProperty.Value.Deserialize(
                        property.PropertyType,
                        JsonSerializerOptions.Web);
                    property.Setter(value, propertyValue);
                    propertyNames.Add(property.PropertyName);
                }
            }
            catch (JsonException exception) {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }

            try {
                int affectedRows = await executor
                    .PatchAsync(value, propertyNames, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return affectedRows == 0 ? Results.NotFound() : Results.NoContent();
            }
            catch (ArgumentException exception) {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }
    }

    private static async Task<IResult> DeleteAsync<TEntity>(
        HttpContext context,
        ICrudExecutor executor,
        CrudEntityDefinition entity,
        CancellationToken cancellationToken)
        where TEntity : class {
        if (!TryReadKeys(context, entity, out Dictionary<string, object?> keys, out string? error))
            return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);

        TEntity? value = await executor
            .FindAsync<TEntity>(keys, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (value is null)
            return Results.NotFound();

        int affectedRows = await executor
            .DeleteAsync(value, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return affectedRows == 0 ? Results.NotFound() : Results.NoContent();
    }

    private static bool TryAssignKeys<TEntity>(
        HttpContext context,
        TEntity value,
        CrudEntityDefinition entity,
        out string? error)
        where TEntity : class {
        if (!TryReadKeys(context, entity, out Dictionary<string, object?> keys, out error))
            return false;

        foreach (CrudPropertyDefinition key in entity.KeyProperties) {
            if (key.Setter is null) {
                error = $"A chave '{key.PropertyName}' precisa ser gravável para operações HTTP.";
                return false;
            }

            key.Setter(value, keys[key.PropertyName]);
        }

        return true;
    }

    private static bool TryReadKeys(
        HttpContext context,
        CrudEntityDefinition entity,
        out Dictionary<string, object?> keys,
        out string? error) {
        keys = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (CrudPropertyDefinition key in entity.KeyProperties) {
            string routeName = JsonPropertyName(key);
            string? rawValue = context.Request.RouteValues[routeName]?.ToString();

            if (string.IsNullOrWhiteSpace(rawValue)) {
                error = $"A chave de rota '{routeName}' não foi informada.";
                return false;
            }

            try {
                keys[key.PropertyName] = ConvertKey(rawValue, key.PropertyType);
            }
            catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException or ArgumentException) {
                error = $"O valor '{rawValue}' não é válido para a chave '{key.PropertyName}'.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static object ConvertKey(string value, Type propertyType) {
        Type targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (targetType == typeof(Guid))
            return Guid.Parse(value);

        if (targetType == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(DateOnly))
            return DateOnly.Parse(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(TimeOnly))
            return TimeOnly.Parse(value, CultureInfo.InvariantCulture);

        if (targetType.IsEnum)
            return Enum.Parse(targetType, value, true);

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static CrudPropertyDefinition? FindProperty(
        CrudEntityDefinition entity,
        string jsonPropertyName) =>
        entity.Properties
            .FirstOrDefault(property =>
                string.Equals(property.PropertyName, jsonPropertyName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(JsonPropertyName(property), jsonPropertyName, StringComparison.OrdinalIgnoreCase));

    private static string JsonPropertyName(CrudPropertyDefinition property) =>
        property.PropertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ??
        JsonNamingPolicy.CamelCase.ConvertName(property.PropertyName);
}
