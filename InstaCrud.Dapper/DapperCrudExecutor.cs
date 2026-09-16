using System.Data;
using System.Globalization;
using Dapper;
using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Sql;
using InstaCrud.Core;
using InstaCrud.Core.Querying;
using InstaCrud.Handler;
using InstaCrud.Interfaces;
using InstaCrud.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper;

public sealed class DapperCrudExecutor : ICrudExecutor {
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
        DynamicParameters parameters = CreateParameters(definition);
        CommandDefinition dapperCommand = CreateCommand(
            definition,
            parameters,
            transaction,
            commandTimeout,
            cancellationToken);

        switch (definition.ResultMode) {
            case SqlCommandResultMode.None:
                return await _connection.ExecuteAsync(dapperCommand).ConfigureAwait(false);

            case SqlCommandResultMode.ScalarResult:
                if (command.ReturningFields.Count != 1) {
                    throw new NotSupportedException(
                        "O retorno escalar suporta exatamente uma propriedade gerada pelo banco.");
                }

                object? value = await _connection
                    .ExecuteScalarAsync<object?>(dapperCommand)
                    .ConfigureAwait(false);
                SetGeneratedValue(
                    entity,
                    command.ReturningFields.Single().ParameterName!,
                    value);
                return 1;

            case SqlCommandResultMode.OutputParameters:
                int affectedRows = await _connection
                    .ExecuteAsync(dapperCommand)
                    .ConfigureAwait(false);

                foreach (SqlOutputParameterDefinition output in definition.OutputParameters) {
                    SetGeneratedValue(
                        entity,
                        output.TargetName,
                        parameters.Get<object?>(output.Name));
                }

                return affectedRows;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(definition),
                    definition.ResultMode,
                    "Modo de resultado SQL desconhecido.");
        }
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
            CreateParameters(definition),
            transaction,
            commandTimeout,
            cancellationToken);
        IEnumerable<TEntity> entities = await _connection
            .QueryAsync<TEntity>(dapperCommand)
            .ConfigureAwait(false);

        return entities.ToArray();
    }

    public async Task<IReadOnlyList<TEntity>> SelectAsync<TEntity>(
        CrudQuery<TEntity> query,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class {
        CrudCommandModel command = _commandFactory.CreateSelect(query);
        SqlCommandDefinition definition = _provider.Build(command);
        CommandDefinition dapperCommand = CreateCommand(
            definition,
            CreateParameters(definition),
            transaction,
            commandTimeout,
            cancellationToken);
        IEnumerable<TEntity> entities = await _connection
            .QueryAsync<TEntity>(dapperCommand)
            .ConfigureAwait(false);

        return entities.ToArray();
    }

    public Task<TEntity?> FindAsync<TEntity>(
        object? keyValue,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class =>
        FindAsync<TEntity>(
            [keyValue],
            transaction,
            commandTimeout,
            cancellationToken);

    public async Task<TEntity?> FindAsync<TEntity>(
        IReadOnlyCollection<object?> keyValues,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class {
        CrudCommandModel command = _commandFactory.CreateSelectByKey<TEntity>(keyValues);
        SqlCommandDefinition definition = _provider.Build(command);

        return await _connection
            .QuerySingleOrDefaultAsync<TEntity>(CreateCommand(
                definition,
                CreateParameters(definition),
                transaction,
                commandTimeout,
                cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<TEntity?> FindAsync<TEntity>(
        IReadOnlyDictionary<string, object?> keyValues,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class {
        CrudCommandModel command = _commandFactory.CreateSelectByKey<TEntity>(keyValues);
        SqlCommandDefinition definition = _provider.Build(command);

        return await _connection
            .QuerySingleOrDefaultAsync<TEntity>(CreateCommand(
                definition,
                CreateParameters(definition),
                transaction,
                commandTimeout,
                cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<long> CountAsync<TEntity>(
        CrudQuery<TEntity>? query = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class {
        CrudCommandModel command = _commandFactory.CreateCount(
            query ?? new CrudQuery<TEntity>());
        SqlCommandDefinition definition = _provider.Build(command);

        return await _connection
            .ExecuteScalarAsync<long>(CreateCommand(
                definition,
                CreateParameters(definition),
                transaction,
                commandTimeout,
                cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<TEntity>> PageAsync<TEntity>(
        CrudQuery<TEntity> query,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TEntity : class {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Pagination is null) {
            throw new ArgumentException(
                "A consulta paginada exige a configuração de Page.",
                nameof(query));
        }

        IReadOnlyList<TEntity> items = await SelectAsync(
            query,
            transaction,
            commandTimeout,
            cancellationToken).ConfigureAwait(false);
        long total = await CountAsync(
            query,
            transaction,
            commandTimeout,
            cancellationToken).ConfigureAwait(false);

        return new PagedResult<TEntity> {
            Items = items,
            Total = total,
            Page = query.Pagination.Page,
            PageSize = query.Pagination.PageSize
        };
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
            CreateCommand(
                definition,
                CreateParameters(definition),
                transaction,
                commandTimeout,
                cancellationToken));
    }

    private static CommandDefinition CreateCommand(
        SqlCommandDefinition definition,
        DynamicParameters parameters,
        IDbTransaction? transaction,
        int? commandTimeout,
        CancellationToken cancellationToken) =>
        new(
            definition.Sql,
            parameters,
            transaction,
            commandTimeout,
            CommandType.Text,
            cancellationToken: cancellationToken);

    private static DynamicParameters CreateParameters(SqlCommandDefinition definition) {
        var parameters = new DynamicParameters();

        foreach ((string name, object? value) in definition.Parameters)
            parameters.Add(name, value);

        foreach (SqlOutputParameterDefinition output in definition.OutputParameters) {
            parameters.Add(
                output.Name,
                dbType: GetDbType(output.ValueType),
                direction: ParameterDirection.Output,
                size: output.ValueType == typeof(string) ? 4000 : null);
        }

        return parameters;
    }

    private void SetGeneratedValue<TEntity>(
        TEntity entity,
        string targetName,
        object? value)
        where TEntity : class {
        CrudPropertyDefinition property = _registry
            .Get(typeof(TEntity))
            .Properties
            .Single(x => string.Equals(
                x.PropertyName,
                targetName,
                StringComparison.Ordinal));

        if (property.Setter is null) {
            throw new InvalidOperationException(
                $"A propriedade gerada '{property.PropertyName}' precisa ser gravável.");
        }

        property.Setter(entity, ConvertValue(value, property.PropertyType));
    }

    private static DbType GetDbType(Type valueType) {
        Type type = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);

        return Type.GetTypeCode(type) switch {
            TypeCode.Boolean => DbType.Boolean,
            TypeCode.Byte => DbType.Byte,
            TypeCode.SByte => DbType.SByte,
            TypeCode.Int16 => DbType.Int16,
            TypeCode.UInt16 => DbType.UInt16,
            TypeCode.Int32 => DbType.Int32,
            TypeCode.UInt32 => DbType.UInt32,
            TypeCode.Int64 => DbType.Int64,
            TypeCode.UInt64 => DbType.UInt64,
            TypeCode.Single => DbType.Single,
            TypeCode.Double => DbType.Double,
            TypeCode.Decimal => DbType.Decimal,
            TypeCode.DateTime => DbType.DateTime,
            TypeCode.Char => DbType.StringFixedLength,
            TypeCode.String => DbType.String,
            _ when type == typeof(Guid) => DbType.Guid,
            _ when type == typeof(byte[]) => DbType.Binary,
            _ when type == typeof(DateTimeOffset) => DbType.DateTimeOffset,
            _ when type == typeof(TimeSpan) => DbType.Time,
            _ => throw new NotSupportedException(
                $"O tipo '{valueType.Name}' não pode ser usado como parâmetro de saída.")
        };
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
