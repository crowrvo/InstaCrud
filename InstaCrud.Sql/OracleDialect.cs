namespace InstaCrud.Sql;

public sealed class OracleDialect : SqlDialect {
    public static OracleDialect Instance { get; } = new();

    private OracleDialect() {
    }

    public override string Name => "Oracle";

    public override string StatementTerminator => string.Empty;

    protected override string ParameterPrefix => ":";

    public override string Pagination(string offsetParameter, string pageSizeParameter) =>
        $" OFFSET {offsetParameter} ROWS FETCH NEXT {pageSizeParameter} ROWS ONLY";

    public override string InsertReturning(IReadOnlyCollection<string> columnNames) {
        if (columnNames.Count > 0) {
            throw new NotSupportedException(
                "Chaves geradas no Oracle exigem parâmetros de saída com RETURNING INTO e ainda não são suportadas.");
        }

        return string.Empty;
    }

    protected override string QuoteIdentifierPart(string identifierPart) =>
        $"\"{identifierPart}\"";
}
