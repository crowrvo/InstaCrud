using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Abstractions.Sql;
using InstaCrud.Dapper.Builders;
using InstaCrud.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper;

public sealed class DapperProvider : ICrudProvider<SqlCommandDefinition> {
    private readonly DapperInsertProvider _insert;
    private readonly DapperSelectProvider _select;
    private readonly DapperUpdateProvider _update;
    private readonly DapperPatchProvider _patch;
    private readonly DapperDeleteProvider _delete;

    public DapperProvider()
        : this(SqlServerDialect.Instance) {
    }

    public DapperProvider(ISqlDialect dialect) {
        ArgumentNullException.ThrowIfNull(dialect);

        _insert = new DapperInsertProvider(dialect);
        _select = new DapperSelectProvider(dialect);
        _update = new DapperUpdateProvider(dialect);
        _patch = new DapperPatchProvider(dialect);
        _delete = new DapperDeleteProvider(dialect);
    }

    public SqlCommandDefinition Build(CrudCommandModel command) {
        ArgumentNullException.ThrowIfNull(command);

        return command.OperationType switch {
            CrudOperationType.Insert => _insert.Build(command),
            CrudOperationType.Select => _select.Build(command),
            CrudOperationType.Count => _select.Build(command),
            CrudOperationType.Update => _update.Build(command),
            CrudOperationType.Patch => _patch.Build(command),
            CrudOperationType.Delete => _delete.Build(command),
            _ => throw new ArgumentOutOfRangeException(
                nameof(command),
                command.OperationType,
                "Operação CRUD desconhecida.")
        };
    }
}
