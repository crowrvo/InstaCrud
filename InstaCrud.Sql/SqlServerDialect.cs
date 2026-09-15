namespace InstaCrud.Sql;

public sealed class SqlServerDialect : SqlDialect {
    public static SqlServerDialect Instance { get; } = new();

    private SqlServerDialect() {
    }

    public override string Name => "SqlServer";

    public override string StatementTerminator => ";";

    protected override string ParameterPrefix => "@";

    public override string Pagination(string offsetParameter, string pageSizeParameter) =>
        $" OFFSET {offsetParameter} ROWS FETCH NEXT {pageSizeParameter} ROWS ONLY";

    public override string InsertReturning(IReadOnlyCollection<string> columnNames) =>
        columnNames.Count == 0
            ? string.Empty
            : " OUTPUT " + string.Join(", ", columnNames.Select(x => $"INSERTED.{Identifier(x)}"));

    protected override string QuoteIdentifierPart(string identifierPart) =>
        $"[{identifierPart}]";
}
