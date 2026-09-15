using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudDeleteProvider<out TResult> {
    TResult Build(CrudCommandModel command);
}
