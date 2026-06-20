using InstaCrud.Decorators;
using InstaCrud.Interfaces;
using System.Reflection;

namespace InstaCrud.Handler;

public sealed class EntityScanner
    : IEntityScanner {
    public IEnumerable<Type> Scan(
        Assembly assembly) {
        return assembly
            .GetTypes()
            .Where(x =>
                x.IsClass &&
                x.GetCustomAttribute<CrudAttribute>() != null);
    }
}