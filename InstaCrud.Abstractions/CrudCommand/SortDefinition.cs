namespace InstaCrud.Abstractions.CrudCommand;

public sealed class SortDefinition {
    public required string ColumnName { get; init; }

    public bool Descending { get; init; }
}
