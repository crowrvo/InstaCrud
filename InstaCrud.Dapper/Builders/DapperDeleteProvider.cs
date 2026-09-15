using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Abstractions.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper.Builders;

public sealed class DapperDeleteProvider : ICrudDeleteProvider<SqlCommandDefinition> {
    public SqlCommandDefinition Build(CrudCommandModel command) {
        DapperSql.ValidateOperation(command, CrudOperationType.Delete);

        if (command.Filters.Count == 0)
            throw new ArgumentException("O delete exige pelo menos um filtro.", nameof(command));

        var parameters = new Dictionary<string, object?>();
        int parameterIndex = 0;
        string where = DapperSql.Where(command.Filters, parameters, ref parameterIndex);

        return new SqlCommandDefinition {
            Sql = $"DELETE FROM {DapperSql.Identifier(command.TableName)}{where};",
            Parameters = parameters
        };
    }
}
