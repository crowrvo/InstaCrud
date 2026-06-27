using InstantCrud.Abstractions.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudUpdateProvider<out TResult> {
    TResult Build(CrudCommand command);
}
