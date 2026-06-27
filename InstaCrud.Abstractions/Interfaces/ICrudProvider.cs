using InstantCrud.Abstractions.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudProvider<out TResult> {
    TResult Build(CrudCommand command);
}
