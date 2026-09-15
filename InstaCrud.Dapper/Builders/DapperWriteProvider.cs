using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper.Builders;

internal static class DapperWriteProvider {
    public static SqlCommandDefinition Build(
        CrudCommandModel command,
        CrudOperationType operationType) {
        DapperSql.ValidateOperation(command, operationType);

        if (command.Fields.Count == 0)
            throw new ArgumentException($"O {operationType} exige pelo menos um campo.", nameof(command));

        if (command.Filters.Count == 0)
            throw new ArgumentException($"O {operationType} exige pelo menos um filtro.", nameof(command));

        DapperSql.ValidateDistinctFields(command.Fields);

        var parameters = new Dictionary<string, object?>();
        int parameterIndex = 0;
        string assignments = string.Join(
            ", ",
            command.Fields.Select(x =>
                $"{DapperSql.Identifier(x.ColumnName)} = " +
                DapperSql.AddParameter(parameters, x.Value, ref parameterIndex)));
        string where = DapperSql.Where(command.Filters, parameters, ref parameterIndex);

        return new SqlCommandDefinition {
            Sql = $"UPDATE {DapperSql.Identifier(command.TableName)} SET {assignments}{where};",
            Parameters = parameters
        };
    }
}
