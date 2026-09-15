using InstaCrud.Abstractions.Attributes;
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
                !x.IsAbstract &&
                x.IsDefined(typeof(CrudAttribute), false));
    }
}
