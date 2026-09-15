using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Abstractions.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper.Builders;

public sealed class DapperUpdateProvider : ICrudUpdateProvider<SqlCommandDefinition> {
    public SqlCommandDefinition Build(CrudCommandModel command) =>
        DapperWriteProvider.Build(command, CrudOperationType.Update);
}
