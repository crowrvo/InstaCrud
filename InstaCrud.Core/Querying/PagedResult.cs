namespace InstaCrud.Core.Querying;

public sealed class PagedResult<TEntity> {
    public required IReadOnlyList<TEntity> Items { get; init; }

    public required long Total { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public long TotalPages =>
        Total == 0 ? 0 : ((Total - 1) / PageSize) + 1;
}
