using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Interfaces;
using InstaCrud.Core.Querying;

namespace InstaCrud.Core;

public sealed class CrudEngine<TResult> {
    private readonly IEntityCommandFactory _commandFactory;
    private readonly ICrudProvider<TResult> _provider;

    public CrudEngine(
        IEntityCommandFactory commandFactory,
        ICrudProvider<TResult> provider) {
        ArgumentNullException.ThrowIfNull(commandFactory);
        ArgumentNullException.ThrowIfNull(provider);

        _commandFactory = commandFactory;
        _provider = provider;
    }

    public TResult Insert<TEntity>(TEntity entity)
        where TEntity : class =>
        _provider.Build(_commandFactory.CreateInsert(entity));

    public TResult Select<TEntity>()
        where TEntity : class =>
        _provider.Build(_commandFactory.CreateSelect<TEntity>());

    public TResult Select<TEntity>(CrudQuery<TEntity> query)
        where TEntity : class =>
        _provider.Build(_commandFactory.CreateSelect(query));

    public TResult SelectByKey<TEntity>(params object?[] keyValues)
        where TEntity : class =>
        _provider.Build(_commandFactory.CreateSelectByKey<TEntity>(keyValues));

    public TResult SelectByKey<TEntity>(IReadOnlyDictionary<string, object?> keyValues)
        where TEntity : class =>
        _provider.Build(_commandFactory.CreateSelectByKey<TEntity>(keyValues));

    public TResult Count<TEntity>()
        where TEntity : class =>
        Count(new CrudQuery<TEntity>());

    public TResult Count<TEntity>(CrudQuery<TEntity> query)
        where TEntity : class =>
        _provider.Build(_commandFactory.CreateCount(query));

    public TResult Update<TEntity>(TEntity entity)
        where TEntity : class =>
        _provider.Build(_commandFactory.CreateUpdate(entity));

    public TResult Patch<TEntity>(
        TEntity entity,
        params string[] propertyNames)
        where TEntity : class =>
        _provider.Build(_commandFactory.CreatePatch(entity, propertyNames));

    public TResult Delete<TEntity>(TEntity entity)
        where TEntity : class =>
        _provider.Build(_commandFactory.CreateDelete(entity));
}
