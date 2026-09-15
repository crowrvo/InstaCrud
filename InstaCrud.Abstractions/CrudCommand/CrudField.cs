namespace InstaCrud.Abstractions.CrudCommand;

public sealed class CrudField {
    public required string ColumnName { get; init; }
    public string? ParameterName { get; init; }
    public Type? ValueType { get; init; }
    public required object? Value { get; init; }
}
