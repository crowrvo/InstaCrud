namespace InstaCrud.Sql;

public interface ISqlDialect {
    string Name { get; }

    string StatementTerminator { get; }

    string Identifier(string identifier);

    string Parameter(string parameterName);

    string Pagination(string offsetParameter, string pageSizeParameter);

    string InsertReturning(IReadOnlyCollection<string> columnNames);
}
