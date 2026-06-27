using InstantCrud.Abstractions.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudDeleteProvider<out TResult> {
    TResult Build(CrudCommand command);
}
