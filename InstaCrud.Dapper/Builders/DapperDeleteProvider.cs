using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Abstractions.Sql;
using InstaCrud.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper.Builders;

public sealed class DapperDeleteProvider : ICrudDeleteProvider<SqlCommandDefinition> {
    private readonly DapperSql _sql;

    public DapperDeleteProvider(ISqlDialect dialect) {
        _sql = new DapperSql(dialect);
    }

    public SqlCommandDefinition Build(CrudCommandModel command) {
        _sql.ValidateOperation(command, CrudOperationType.Delete);

        if (command.Filters.Count == 0)
            throw new ArgumentException("O delete exige pelo menos um filtro.", nameof(command));

        var parameters = new Dictionary<string, object?>();
        int parameterIndex = 0;
        string where = _sql.Where(command.Filters, parameters, ref parameterIndex);

        return new SqlCommandDefinition {
            Sql = $"DELETE FROM {_sql.Identifier(command.TableName)}{where}{_sql.StatementTerminator}",
            Parameters = parameters
        };
    }
}
