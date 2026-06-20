namespace InstantCrud.Abstractions.CrudCommand;

public sealed class PaginationDefinition {
    public int Page { get; init; }

    public int PageSize { get; init; }
}
