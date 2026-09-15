namespace InstaCrud.Sql;

using InstaCrud.Abstractions.Sql;

public sealed class SqlServerDialect : SqlDialect {
    public static SqlServerDialect Instance { get; } = new();

    private SqlServerDialect() {
    }

    public override string Name => "SqlServer";

    public override string StatementTerminator => ";";

    protected override string ParameterPrefix => "@";

    public override string Pagination(string offsetParameter, string pageSizeParameter) =>
        $" OFFSET {offsetParameter} ROWS FETCH NEXT {pageSizeParameter} ROWS ONLY";

    public override SqlInsertReturningDefinition InsertReturning(
        IReadOnlyCollection<SqlReturningField> fields) =>
        fields.Count == 0
            ? SqlInsertReturningDefinition.Empty
            : new SqlInsertReturningDefinition {
                BeforeValues = " OUTPUT " + string.Join(
                    ", ",
                    fields.Select(x => $"INSERTED.{Identifier(x.ColumnName)}")),
                ResultMode = SqlCommandResultMode.ScalarResult
            };

    protected override string QuoteIdentifierPart(string identifierPart) =>
        $"[{identifierPart}]";
}
