using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Interfaces;

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
