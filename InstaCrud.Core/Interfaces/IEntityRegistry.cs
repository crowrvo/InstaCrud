using InstaCrud.Core;

namespace InstaCrud.Interfaces;

public interface IEntityRegistry {
    IReadOnlyCollection<CrudEntityDefinition> Entities { get; }

    CrudEntityDefinition Get(Type type);
}