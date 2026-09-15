using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudInsertProvider<out TResult> {
    TResult Build(CrudCommandModel command);
}
