namespace InstaCrud.Core.Querying;

public sealed record EntityQuerySort(
    string PropertyName,
    bool Descending);
