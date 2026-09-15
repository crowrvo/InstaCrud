using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudPatchProvider<out TResult> {
    TResult Build(CrudCommandModel command);
}
