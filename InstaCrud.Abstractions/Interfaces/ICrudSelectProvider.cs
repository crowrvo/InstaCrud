using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudSelectProvider<out TResult> {
    TResult Build(CrudCommandModel command);
}
