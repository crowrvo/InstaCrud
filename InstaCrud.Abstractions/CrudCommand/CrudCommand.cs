namespace InstantCrud.Abstractions.CrudCommand;

public sealed class CrudCommand {
    public required CrudOperationType OperationType { get; init; }

    public required string TableName { get; init; }

    public IReadOnlyCollection<CrudField> Fields { get; init; }
        = [];

    public IReadOnlyCollection<CrudFilter> Filters { get; init; }
        = [];

    public IReadOnlyCollection<SortDefinition> Sorts { get; init; }
        = [];

    public PaginationDefinition? Pagination { get; init; }
}
