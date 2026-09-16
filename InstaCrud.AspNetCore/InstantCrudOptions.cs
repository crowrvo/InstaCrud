using System.Reflection;
using InstaCrud.Handler;
using InstaCrud.Interfaces;

namespace InstaCrud.AspNetCore;

public sealed class InstantCrudOptions {
    private readonly HashSet<Type> _entityTypes = [];

    internal IReadOnlyCollection<Type> EntityTypes => _entityTypes;

    internal Func<IServiceProvider, IEntityRegistry, ICrudExecutor>? ExecutorFactory { get; private set; }

    public string RoutePrefix { get; set; } = "/api";

    public bool MapDocumentation { get; set; } = true;

    public string OpenApiPattern { get; set; } = "/openapi/{documentName}.json";

    public string ScalarPath { get; set; } = "/scalar";

    public string ApiTitle { get; set; } = "InstantCrud API";

    public InstantCrudOptions AddEntity<TEntity>()
        where TEntity : class {
        _entityTypes.Add(typeof(TEntity));
        return this;
    }

    public InstantCrudOptions AddEntities(params Type[] entityTypes) {
        ArgumentNullException.ThrowIfNull(entityTypes);

        foreach (Type entityType in entityTypes) {
            ArgumentNullException.ThrowIfNull(entityType);
            _entityTypes.Add(entityType);
        }

        return this;
    }

    public InstantCrudOptions AddEntitiesFromAssembly(Assembly assembly) {
        ArgumentNullException.ThrowIfNull(assembly);

        var scanner = new EntityScanner();
        return AddEntities(scanner.Scan(assembly).ToArray());
    }

    public InstantCrudOptions UseExecutor(
        Func<IServiceProvider, IEntityRegistry, ICrudExecutor> executorFactory) {
        ArgumentNullException.ThrowIfNull(executorFactory);
        ExecutorFactory = executorFactory;
        return this;
    }

    internal void Validate() {
        if (_entityTypes.Count == 0)
            throw new InvalidOperationException("Ao menos uma entidade CRUD precisa ser registrada.");

        if (ExecutorFactory is null)
            throw new InvalidOperationException("Um executor CRUD precisa ser configurado com UseExecutor.");

        ValidateRoute(RoutePrefix, nameof(RoutePrefix));

        if (!MapDocumentation)
            return;

        ValidateRoute(OpenApiPattern, nameof(OpenApiPattern));
        ValidateRoute(ScalarPath, nameof(ScalarPath));

        if (!OpenApiPattern.Contains("{documentName}", StringComparison.Ordinal)) {
            throw new InvalidOperationException(
                "OpenApiPattern precisa conter o parâmetro '{documentName}'.");
        }
    }

    private static void ValidateRoute(string route, string propertyName) {
        if (string.IsNullOrWhiteSpace(route) || !route.StartsWith('/')) {
            throw new InvalidOperationException(
                $"{propertyName} precisa ser uma rota absoluta iniciada por '/'.");
        }
    }
}
