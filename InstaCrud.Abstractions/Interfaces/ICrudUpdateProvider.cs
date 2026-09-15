using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudUpdateProvider<out TResult> {
    TResult Build(CrudCommandModel command);
}
