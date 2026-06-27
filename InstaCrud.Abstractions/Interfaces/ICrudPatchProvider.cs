using InstantCrud.Abstractions.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudPatchProvider<out TResult> {
    TResult Build(CrudCommand command);
}
