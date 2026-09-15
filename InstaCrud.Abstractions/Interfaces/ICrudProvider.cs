using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudProvider<out TResult> {
    TResult Build(CrudCommandModel command);
}
