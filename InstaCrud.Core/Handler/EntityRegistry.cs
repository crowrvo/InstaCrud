using InstaCrud.Core;

namespace InstaCrud.Handler;

public sealed class EntityRegistry {
    private readonly Dictionary<Type, CrudEntityDefinition>
        _entities = [];
}