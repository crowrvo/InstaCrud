using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Abstractions.Sql;
using InstaCrud.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper.Builders;

public sealed class DapperUpdateProvider : ICrudUpdateProvider<SqlCommandDefinition> {
    private readonly ISqlDialect _dialect;

    public DapperUpdateProvider(ISqlDialect dialect) {
        _dialect = dialect;
    }

    public SqlCommandDefinition Build(CrudCommandModel command) =>
        DapperWriteProvider.Build(command, CrudOperationType.Update, _dialect);
}
