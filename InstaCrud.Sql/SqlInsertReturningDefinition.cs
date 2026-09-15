using InstaCrud.Abstractions.Sql;

namespace InstaCrud.Sql;

public sealed class SqlInsertReturningDefinition {
    public static SqlInsertReturningDefinition Empty { get; } = new();

    public string BeforeValues { get; init; } = string.Empty;

    public string AfterValues { get; init; } = string.Empty;

    public SqlCommandResultMode ResultMode { get; init; }

    public IReadOnlyCollection<SqlOutputParameterDefinition> OutputParameters { get; init; }
        = [];
}
