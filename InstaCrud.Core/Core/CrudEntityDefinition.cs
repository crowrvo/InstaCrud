namespace InstaCrud.Core;

public sealed class CrudEntityDefinition {
    public required Type EntityType { get; init; }

    public required string RouteName { get; init; }

    public required string TableName { get; init; }

    public required IReadOnlyCollection<CrudPropertyDefinition> Properties { get; init; }

    public required IReadOnlyCollection<CrudPropertyDefinition> KeyProperties { get; init; }

    public bool HasKey =>
        KeyProperties.Count > 0;
}
