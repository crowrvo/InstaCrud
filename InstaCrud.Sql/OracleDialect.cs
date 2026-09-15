namespace InstaCrud.Sql;

using InstaCrud.Abstractions.Sql;

public sealed class OracleDialect : SqlDialect {
    public static OracleDialect Instance { get; } = new();

    private OracleDialect() {
    }

    public override string Name => "Oracle";

    public override string StatementTerminator => string.Empty;

    protected override string ParameterPrefix => ":";

    public override string Pagination(string offsetParameter, string pageSizeParameter) =>
        $" OFFSET {offsetParameter} ROWS FETCH NEXT {pageSizeParameter} ROWS ONLY";

    public override SqlInsertReturningDefinition InsertReturning(
        IReadOnlyCollection<SqlReturningField> fields) {
        if (fields.Count == 0)
            return SqlInsertReturningDefinition.Empty;

        SqlReturningField[] returningFields = fields.ToArray();
        SqlOutputParameterDefinition[] outputParameters = returningFields
            .Select((field, index) => new SqlOutputParameterDefinition {
                Name = $"out{index}",
                TargetName = field.TargetName,
                ValueType = field.ValueType
            })
            .ToArray();

        return new SqlInsertReturningDefinition {
            AfterValues = " RETURNING " +
                string.Join(", ", returningFields.Select(x => Identifier(x.ColumnName))) +
                " INTO " +
                string.Join(", ", outputParameters.Select(x => Parameter(x.Name))),
            ResultMode = SqlCommandResultMode.OutputParameters,
            OutputParameters = outputParameters
        };
    }

    protected override string QuoteIdentifierPart(string identifierPart) =>
        $"\"{identifierPart}\"";
}
