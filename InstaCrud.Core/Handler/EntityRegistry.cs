using InstaCrud.Core;
using InstaCrud.Interfaces;

namespace InstaCrud.Handler;

public sealed class EntityRegistry : IEntityRegistry {
    private readonly IReadOnlyDictionary<Type, CrudEntityDefinition> _entitiesByType;
    private readonly IReadOnlyDictionary<string, CrudEntityDefinition> _entitiesByRoute;

    public EntityRegistry(IEnumerable<CrudEntityDefinition> entities) {
        ArgumentNullException.ThrowIfNull(entities);

        CrudEntityDefinition[] definitions = entities.ToArray();

        _entitiesByType = definitions.ToDictionary(x => x.EntityType);
        _entitiesByRoute = definitions.ToDictionary(
            x => x.RouteName,
            StringComparer.OrdinalIgnoreCase);
        Entities = Array.AsReadOnly(definitions);
    }

    public IReadOnlyCollection<CrudEntityDefinition> Entities { get; }

    public CrudEntityDefinition Get(Type type) {
        ArgumentNullException.ThrowIfNull(type);

        return _entitiesByType.TryGetValue(type, out CrudEntityDefinition? entity)
            ? entity
            : throw new KeyNotFoundException(
                $"A entidade '{type.Name}' não está registrada.");
    }

    public CrudEntityDefinition Get(string routeName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeName);

        return _entitiesByRoute.TryGetValue(routeName, out CrudEntityDefinition? entity)
            ? entity
            : throw new KeyNotFoundException(
                $"A rota '{routeName}' não está registrada.");
    }
}
