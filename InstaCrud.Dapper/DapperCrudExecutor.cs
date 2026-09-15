using System.Data;
using System.Globalization;
using Dapper;
using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Sql;
using InstaCrud.Core;
using InstaCrud.Handler;
using InstaCrud.Interfaces;
using InstaCrud.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper;

public sealed class DapperCrudExecutor {
    private readonly IDbConnection _connection;
    private readonly IEntityRegistry _registry;
    private readonly IEntityCommandFactory _commandFactory;
    private readonly DapperProvider _provider;

    public DapperCrudExecutor(
        IDbConnection connection,
        IEntityRegistry registry)
        : this(connection, registry, SqlServerDialect.Instance) {
    }

    public DapperCrudExecutor(
        IDbConnection connection,
        IEntityRegistry registry,
        ISqlDialect dialect) {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(dialect);

        _connection = connection;
        _registry = registry;
        _commandFactory = new EntityCommandFactory(registry);
        _provider = new DapperProvider(dialect);
    }

    public async Task<int> InsertAsync<TEntity>(
        TEntity entity,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class {
        CrudCommandModel command = _commandFactory.CreateInsert(entity);
        SqlCommandDefinition definition = _provider.Build(command);
        CommandDefinition dapperCommand = CreateCommand(
            definition,
            transaction,
            commandTimeout,
            cancellationToken);

        if (command.ReturningFields.Count == 0)
            return await _connection.ExecuteAsync(dapperCommand).ConfigureAwait(false);

        if (command.ReturningFields.Count > 1) {
            throw new NotSupportedException(
                "A atribuição automática suporta apenas uma propriedade gerada pelo banco.");
        }

        object? value = await _connection
            .ExecuteScalarAsync<object?>(dapperCommand)
            .ConfigureAwait(false);
        SetGeneratedValue(entity, command.ReturningFields.Single(), value);
        return 1;
    }

    public async Task<IReadOnlyList<TEntity>> SelectAsync<TEntity>(
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class {
        CrudCommandModel command = _commandFactory.CreateSelect<TEntity>();
        SqlCommandDefinition definition = _provider.Build(command);
        CommandDefinition dapperCommand = CreateCommand(
            definition,
            transaction,
            commandTimeout,
            cancellationToken);
        IEnumerable<TEntity> entities = await _connection
            .QueryAsync<TEntity>(dapperCommand)
            .ConfigureAwait(false);

        return entities.ToArray();
    }

    public Task<int> UpdateAsync<TEntity>(
        TEntity entity,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class =>
        ExecuteAsync(
            _commandFactory.CreateUpdate(entity),
            transaction,
            commandTimeout,
            cancellationToken);

    public Task<int> PatchAsync<TEntity>(
        TEntity entity,
        IReadOnlyCollection<string> propertyNames,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class =>
        ExecuteAsync(
            _commandFactory.CreatePatch(entity, propertyNames),
            transaction,
            commandTimeout,
            cancellationToken);

    public Task<int> DeleteAsync<TEntity>(
        TEntity entity,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class =>
        ExecuteAsync(
            _commandFactory.CreateDelete(entity),
            transaction,
            commandTimeout,
            cancellationToken);

    private Task<int> ExecuteAsync(
        CrudCommandModel command,
        IDbTransaction? transaction,
        int? commandTimeout,
        CancellationToken cancellationToken) {
        SqlCommandDefinition definition = _provider.Build(command);
        return _connection.ExecuteAsync(
            CreateCommand(definition, transaction, commandTimeout, cancellationToken));
    }

    private static CommandDefinition CreateCommand(
        SqlCommandDefinition definition,
        IDbTransaction? transaction,
        int? commandTimeout,
        CancellationToken cancellationToken) =>
        new(
            definition.Sql,
            CreateParameters(definition.Parameters),
            transaction,
            commandTimeout,
            CommandType.Text,
            cancellationToken: cancellationToken);

    private static DynamicParameters CreateParameters(
        IReadOnlyDictionary<string, object?> values) {
        var parameters = new DynamicParameters();

        foreach ((string name, object? value) in values)
            parameters.Add(name, value);

        return parameters;
    }

    private void SetGeneratedValue<TEntity>(
        TEntity entity,
        CrudField returningField,
        object? value)
        where TEntity : class {
        CrudPropertyDefinition property = _registry
            .Get(typeof(TEntity))
            .Properties
            .Single(x => string.Equals(
                x.PropertyName,
                returningField.ParameterName,
                StringComparison.Ordinal));

        if (property.Setter is null) {
            throw new InvalidOperationException(
                $"A propriedade gerada '{property.PropertyName}' precisa ser gravável.");
        }

        property.Setter(entity, ConvertValue(value, property.PropertyType));
    }

    private static object? ConvertValue(object? value, Type propertyType) {
        if (value is null || value is DBNull)
            return null;

        Type targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (targetType.IsInstanceOfType(value))
            return value;

        if (targetType.IsEnum)
            return Enum.ToObject(targetType, value);

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}
