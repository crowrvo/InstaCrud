namespace InstantCrud.Abstractions.CrudCommand;

public sealed class CrudField {
    public required string ColumnName { get; init; }

    public required object? Value { get; init; }
}
