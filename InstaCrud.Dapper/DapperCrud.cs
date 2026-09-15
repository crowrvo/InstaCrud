using InstaCrud.Abstractions.Sql;
using InstaCrud.Core;
using InstaCrud.Handler;
using InstaCrud.Sql;
using System.Data;
using System.Reflection;

namespace InstaCrud.Dapper;

public static class DapperCrud {
    public static CrudEngine<SqlCommandDefinition> Create<TEntity>()
        where TEntity : class =>
        Create(typeof(TEntity));

    public static CrudEngine<SqlCommandDefinition> Create<TEntity>(ISqlDialect dialect)
        where TEntity : class =>
        Create(dialect, typeof(TEntity));

    public static CrudEngine<SqlCommandDefinition> Create(params Type[] entityTypes) {
        EntityRegistry registry = EntityRegistry.FromTypes(entityTypes);
        return Create(registry, SqlServerDialect.Instance);
    }

    public static CrudEngine<SqlCommandDefinition> Create(
        ISqlDialect dialect,
        params Type[] entityTypes) {
        EntityRegistry registry = EntityRegistry.FromTypes(entityTypes);
        return Create(registry, dialect);
    }

    public static DapperCrudExecutor CreateExecutor<TEntity>(IDbConnection connection)
        where TEntity : class =>
        CreateExecutor(connection, typeof(TEntity));

    public static DapperCrudExecutor CreateExecutor<TEntity>(
        IDbConnection connection,
        ISqlDialect dialect)
        where TEntity : class =>
        CreateExecutor(connection, dialect, typeof(TEntity));

    public static DapperCrudExecutor CreateExecutor(
        IDbConnection connection,
        params Type[] entityTypes) =>
        new(connection, EntityRegistry.FromTypes(entityTypes));

    public static DapperCrudExecutor CreateExecutor(
        IDbConnection connection,
        ISqlDialect dialect,
        params Type[] entityTypes) =>
        new(connection, EntityRegistry.FromTypes(entityTypes), dialect);

    public static DapperCrudExecutor CreateExecutorFromAssemblies(
        IDbConnection connection,
        params Assembly[] assemblies) =>
        new(connection, EntityRegistry.FromAssemblies(assemblies));

    public static DapperCrudExecutor CreateExecutorFromAssemblies(
        IDbConnection connection,
        ISqlDialect dialect,
        params Assembly[] assemblies) =>
        new(connection, EntityRegistry.FromAssemblies(assemblies), dialect);

    public static CrudEngine<SqlCommandDefinition> FromAssemblies(params Assembly[] assemblies) {
        EntityRegistry registry = EntityRegistry.FromAssemblies(assemblies);
        return Create(registry, SqlServerDialect.Instance);
    }

    public static CrudEngine<SqlCommandDefinition> FromAssemblies(
        ISqlDialect dialect,
        params Assembly[] assemblies) {
        EntityRegistry registry = EntityRegistry.FromAssemblies(assemblies);
        return Create(registry, dialect);
    }

    private static CrudEngine<SqlCommandDefinition> Create(
        EntityRegistry registry,
        ISqlDialect dialect) =>
        new(
            new EntityCommandFactory(registry),
            new DapperProvider(dialect));
}
