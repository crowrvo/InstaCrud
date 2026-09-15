using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;
using InstaCrud.Core.Querying;

namespace InstaCrud.Interfaces;

public interface IEntityCommandFactory {
    CrudCommandModel CreateInsert<TEntity>(TEntity entity)
        where TEntity : class;

    CrudCommandModel CreateSelect<TEntity>()
        where TEntity : class;

    CrudCommandModel CreateSelect<TEntity>(CrudQuery<TEntity> query)
        where TEntity : class;

    CrudCommandModel CreateUpdate<TEntity>(TEntity entity)
        where TEntity : class;

    CrudCommandModel CreatePatch<TEntity>(
        TEntity entity,
        IReadOnlyCollection<string> propertyNames)
        where TEntity : class;

    CrudCommandModel CreateDelete<TEntity>(TEntity entity)
        where TEntity : class;
}
