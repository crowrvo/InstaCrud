using InstantCrud.Abstractions.CrudCommand;

namespace InstaCrud.Abstractions.Interfaces;

public interface ICrudSelectProvider<out TResult> {
    TResult Build(CrudCommand command);
}
