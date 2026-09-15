using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Abstractions.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper.Builders;

public sealed class DapperSelectProvider : ICrudSelectProvider<SqlCommandDefinition> {
    public SqlCommandDefinition Build(CrudCommandModel command) {
        DapperSql.ValidateOperation(command, CrudOperationType.Select);
        DapperSql.ValidateDistinctFields(command.Fields);

        var parameters = new Dictionary<string, object?>();
        int parameterIndex = 0;
        string columns = command.Fields.Count == 0
            ? "*"
            : string.Join(", ", command.Fields.Select(SelectExpression));
        string where = DapperSql.Where(command.Filters, parameters, ref parameterIndex);
        string orderBy = DapperSql.OrderBy(command.Sorts);
        string pagination = DapperSql.Pagination(
            command.Pagination,
            command.Sorts,
            parameters,
            ref parameterIndex);

        return new SqlCommandDefinition {
            Sql = $"SELECT {columns} FROM {DapperSql.Identifier(command.TableName)}{where}{orderBy}{pagination};",
            Parameters = parameters
        };
    }

    private static string SelectExpression(CrudField field) {
        string column = DapperSql.Identifier(field.ColumnName);

        if (string.IsNullOrWhiteSpace(field.ParameterName) ||
            string.Equals(field.ColumnName, field.ParameterName, StringComparison.OrdinalIgnoreCase)) {
            return column;
        }

        return $"{column} AS {DapperSql.Identifier(field.ParameterName)}";
    }
}
