namespace InstaCrud.Abstractions.Sql;

public sealed class SqlOutputParameterDefinition {
    public required string Name { get; init; }

    public required string TargetName { get; init; }

    public required Type ValueType { get; init; }
}
