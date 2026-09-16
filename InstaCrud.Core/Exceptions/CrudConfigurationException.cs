namespace InstaCrud.Exceptions;

public sealed class CrudConfigurationException : InvalidOperationException {
    public CrudConfigurationException(
        string message,
        Type? entityType = null,
        string? propertyName = null,
        Exception? innerException = null)
        : base(message, innerException) {
        EntityType = entityType;
        PropertyName = propertyName;
    }

    public Type? EntityType { get; }

    public string? PropertyName { get; }
}
