namespace InstaCrud.Abstractions.Sql;

public sealed class SqlCommandDefinition {
    public required string Sql { get; init; }

    public IReadOnlyDictionary<string, object?> Parameters { get; init; }
        = new Dictionary<string, object?>();

    public SqlCommandResultMode ResultMode { get; init; }

    public IReadOnlyCollection<SqlOutputParameterDefinition> OutputParameters { get; init; }
        = [];
}
