using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Abstractions.Sql;
using InstaCrud.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper.Builders;

public sealed class DapperSelectProvider : ICrudSelectProvider<SqlCommandDefinition> {
    private readonly DapperSql _sql;

    public DapperSelectProvider(ISqlDialect dialect) {
        _sql = new DapperSql(dialect);
    }

    public SqlCommandDefinition Build(CrudCommandModel command) {
        _sql.ValidateOperation(command, CrudOperationType.Select);
        DapperSql.ValidateDistinctFields(command.Fields);

        var parameters = new Dictionary<string, object?>();
        int parameterIndex = 0;
        string columns = command.Fields.Count == 0
            ? "*"
            : string.Join(", ", command.Fields.Select(SelectExpression));
        string where = _sql.Where(command.Filters, parameters, ref parameterIndex);
        string orderBy = _sql.OrderBy(command.Sorts);
        string pagination = _sql.Pagination(
            command.Pagination,
            command.Sorts,
            parameters,
            ref parameterIndex);

        return new SqlCommandDefinition {
            Sql = $"SELECT {columns} FROM {_sql.Identifier(command.TableName)}{where}{orderBy}{pagination}{_sql.StatementTerminator}",
            Parameters = parameters
        };
    }

    private string SelectExpression(CrudField field) {
        string column = _sql.Identifier(field.ColumnName);

        if (string.IsNullOrWhiteSpace(field.ParameterName) ||
            string.Equals(field.ColumnName, field.ParameterName, StringComparison.OrdinalIgnoreCase)) {
            return column;
        }

        return $"{column} AS {_sql.Identifier(field.ParameterName)}";
    }
}
