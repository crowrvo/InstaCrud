using InstaCrud.Abstractions.Sql;
using InstaCrud.Core;
using InstaCrud.Handler;
using System.Data;
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

    public static DapperCrudExecutor CreateExecutor<TEntity>(IDbConnection connection)
        where TEntity : class =>
        CreateExecutor(connection, typeof(TEntity));

    public static DapperCrudExecutor CreateExecutor(
        IDbConnection connection,
        params Type[] entityTypes) =>
        new(connection, EntityRegistry.FromTypes(entityTypes));

    public static DapperCrudExecutor CreateExecutorFromAssemblies(
        IDbConnection connection,
        params Assembly[] assemblies) =>
        new(connection, EntityRegistry.FromAssemblies(assemblies));

    public static CrudEngine<SqlCommandDefinition> FromAssemblies(params Assembly[] assemblies) {
        EntityRegistry registry = EntityRegistry.FromAssemblies(assemblies);
        return Create(registry);
    }

    private static CrudEngine<SqlCommandDefinition> Create(EntityRegistry registry) =>
        new(
            new EntityCommandFactory(registry),
            new DapperProvider());
}
