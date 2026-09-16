namespace InstaCrud.Exceptions;

public sealed class EntityNotRegisteredException : KeyNotFoundException {
    public EntityNotRegisteredException(Type entityType)
        : base($"A entidade '{entityType.Name}' não está registrada.") {
        EntityType = entityType;
    }

    public EntityNotRegisteredException(string routeName)
        : base($"A rota '{routeName}' não está registrada.") {
        RouteName = routeName;
    }

    public Type? EntityType { get; }

    public string? RouteName { get; }
}
