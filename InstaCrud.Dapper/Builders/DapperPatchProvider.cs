using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Abstractions.Sql;
using InstaCrud.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper.Builders;

public sealed class DapperPatchProvider : ICrudPatchProvider<SqlCommandDefinition> {
    private readonly ISqlDialect _dialect;

    public DapperPatchProvider(ISqlDialect dialect) {
        _dialect = dialect;
    }

    public SqlCommandDefinition Build(CrudCommandModel command) =>
        DapperWriteProvider.Build(command, CrudOperationType.Patch, _dialect);
}
