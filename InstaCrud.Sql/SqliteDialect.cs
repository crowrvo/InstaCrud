using InstaCrud.Abstractions.Sql;

namespace InstaCrud.Sql;

public sealed class SqliteDialect : SqlDialect {
    public static SqliteDialect Instance { get; } = new();

    private SqliteDialect() {
    }

    public override string Name => "SQLite";

    public override string StatementTerminator => ";";

    protected override string ParameterPrefix => "@";

    public override string Pagination(string offsetParameter, string pageSizeParameter) =>
        $" LIMIT {pageSizeParameter} OFFSET {offsetParameter}";

    public override SqlInsertReturningDefinition InsertReturning(
        IReadOnlyCollection<SqlReturningField> fields) =>
        fields.Count == 0
            ? SqlInsertReturningDefinition.Empty
            : new SqlInsertReturningDefinition {
                AfterValues = " RETURNING " + string.Join(
                    ", ",
                    fields.Select(x => Identifier(x.ColumnName))),
                ResultMode = SqlCommandResultMode.ScalarResult
            };

    protected override string QuoteIdentifierPart(string identifierPart) =>
        $"\"{identifierPart}\"";
}
