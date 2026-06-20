using System.Reflection;

namespace InstaCrud.Interfaces;

public interface IEntityScanner {
    IEnumerable<Type> Scan(Assembly assembly);
}