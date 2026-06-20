namespace InstantCrud.Abstractions.CrudCommand;

public sealed class CrudFilter {
    public required string ColumnName { get; init; }

    public required CrudFilterOperator Operator { get; init; }

    public required object? Value { get; init; }
}