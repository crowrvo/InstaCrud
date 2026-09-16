using InstaCrud.Core;
using InstaCrud.Interfaces;
using InstaCrud.Exceptions;
using System.Reflection;

namespace InstaCrud.Handler;

public sealed class EntityRegistry : IEntityRegistry {
    private readonly IReadOnlyDictionary<Type, CrudEntityDefinition> _entitiesByType;
    private readonly IReadOnlyDictionary<string, CrudEntityDefinition> _entitiesByRoute;

    public EntityRegistry(IEnumerable<CrudEntityDefinition> entities) {
        ArgumentNullException.ThrowIfNull(entities);

        CrudEntityDefinition[] definitions = entities.ToArray();

        ValidarDefinicoes(definitions);

        _entitiesByType = definitions.ToDictionary(x => x.EntityType);
        _entitiesByRoute = definitions.ToDictionary(
            x => x.RouteName,
            StringComparer.OrdinalIgnoreCase);
        Entities = Array.AsReadOnly(definitions);
    }

    public IReadOnlyCollection<CrudEntityDefinition> Entities { get; }

    public static EntityRegistry FromTypes(params Type[] entityTypes) {
        ArgumentNullException.ThrowIfNull(entityTypes);

        var builder = new EntityDefinitionBuilder();
        return new EntityRegistry(
            entityTypes
                .Distinct()
                .Select(builder.Build));
    }

    public static EntityRegistry FromAssemblies(params Assembly[] assemblies) {
        ArgumentNullException.ThrowIfNull(assemblies);

        var scanner = new EntityScanner();
        return FromTypes(
            assemblies
                .SelectMany(scanner.Scan)
                .Distinct()
                .ToArray());
    }

    public CrudEntityDefinition Get(Type type) {
        ArgumentNullException.ThrowIfNull(type);

        return _entitiesByType.TryGetValue(type, out CrudEntityDefinition? entity)
            ? entity
            : throw new EntityNotRegisteredException(type);
    }

    public CrudEntityDefinition Get(string routeName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeName);

        return _entitiesByRoute.TryGetValue(routeName, out CrudEntityDefinition? entity)
            ? entity
            : throw new EntityNotRegisteredException(routeName);
    }

    private static void ValidarDefinicoes(
        IReadOnlyCollection<CrudEntityDefinition> definitions) {
        CrudEntityDefinition? invalidRoute = definitions.FirstOrDefault(x =>
            string.IsNullOrWhiteSpace(x.RouteName));

        if (invalidRoute is not null) {
            throw new CrudConfigurationException(
                $"A rota da entidade '{invalidRoute.EntityType.Name}' não pode ser vazia.",
                invalidRoute.EntityType);
        }

        IGrouping<Type, CrudEntityDefinition>? duplicateType = definitions
            .GroupBy(x => x.EntityType)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicateType is not null) {
            throw new CrudConfigurationException(
                $"A entidade '{duplicateType.Key.Name}' foi registrada mais de uma vez.",
                duplicateType.Key);
        }

        IGrouping<string, CrudEntityDefinition>? duplicateRoute = definitions
            .GroupBy(x => x.RouteName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicateRoute is not null) {
            string entities = string.Join(
                ", ",
                duplicateRoute.Select(x => x.EntityType.Name));
            throw new CrudConfigurationException(
                $"A rota '{duplicateRoute.Key}' está associada a mais de uma entidade: {entities}.");
        }
    }
}
