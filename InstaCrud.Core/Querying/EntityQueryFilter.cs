using InstaCrud.Abstractions.CrudCommand;

namespace InstaCrud.Core.Querying;

public sealed record EntityQueryFilter(
    string PropertyName,
    CrudFilterOperator Operator,
    object? Value);
