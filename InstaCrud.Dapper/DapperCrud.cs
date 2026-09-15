using InstaCrud.Abstractions.Sql;
using InstaCrud.Core;
using InstaCrud.Handler;
using System.Reflection;

namespace InstaCrud.Dapper;

public static class DapperCrud {
    public static CrudEngine<SqlCommandDefinition> Create<TEntity>()
        where TEntity : class =>
        Create(typeof(TEntity));

    public static CrudEngine<SqlCommandDefinition> Create(params Type[] entityTypes) {
        EntityRegistry registry = EntityRegistry.FromTypes(entityTypes);
        return Create(registry);
    }

    public static CrudEngine<SqlCommandDefinition> FromAssemblies(params Assembly[] assemblies) {
        EntityRegistry registry = EntityRegistry.FromAssemblies(assemblies);
        return Create(registry);
    }

    private static CrudEngine<SqlCommandDefinition> Create(EntityRegistry registry) =>
        new(
            new EntityCommandFactory(registry),
            new DapperProvider());
}
