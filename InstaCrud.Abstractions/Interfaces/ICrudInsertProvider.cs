using InstantCrud.Abstractions.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudInsertProvider<out TResult> {
    TResult Build(CrudCommand command);
}
