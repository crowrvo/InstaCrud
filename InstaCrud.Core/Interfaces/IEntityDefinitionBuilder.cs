using InstaCrud.Core;

namespace InstaCrud.Interfaces;

public interface IEntityDefinitionBuilder {
    CrudEntityDefinition Build(Type entityType);
}