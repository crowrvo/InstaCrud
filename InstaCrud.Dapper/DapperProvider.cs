using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Abstractions.Sql;
using InstaCrud.Dapper.Builders;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper;

public sealed class DapperProvider : ICrudProvider<SqlCommandDefinition> {
    private readonly DapperInsertProvider _insert = new();
    private readonly DapperSelectProvider _select = new();
    private readonly DapperUpdateProvider _update = new();
    private readonly DapperPatchProvider _patch = new();
    private readonly DapperDeleteProvider _delete = new();

    public SqlCommandDefinition Build(CrudCommandModel command) {
        ArgumentNullException.ThrowIfNull(command);

        return command.OperationType switch {
            CrudOperationType.Insert => _insert.Build(command),
            CrudOperationType.Select => _select.Build(command),
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
