using System.Data;
using InstaCrud.Core.Querying;

namespace InstaCrud.Interfaces;

public interface ICrudExecutor {
    Task<int> InsertAsync<TEntity>(
        TEntity entity,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<IReadOnlyList<TEntity>> SelectAsync<TEntity>(
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<IReadOnlyList<TEntity>> SelectAsync<TEntity>(
        CrudQuery<TEntity> query,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<TEntity?> FindAsync<TEntity>(
        object? keyValue,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<TEntity?> FindAsync<TEntity>(
        IReadOnlyCollection<object?> keyValues,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<TEntity?> FindAsync<TEntity>(
        IReadOnlyDictionary<string, object?> keyValues,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<long> CountAsync<TEntity>(
        CrudQuery<TEntity>? query = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<PagedResult<TEntity>> PageAsync<TEntity>(
        CrudQuery<TEntity> query,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<int> UpdateAsync<TEntity>(
        TEntity entity,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<int> PatchAsync<TEntity>(
        TEntity entity,
        IReadOnlyCollection<string> propertyNames,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<int> DeleteAsync<TEntity>(
        TEntity entity,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class;
}
