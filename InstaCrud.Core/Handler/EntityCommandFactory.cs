using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Core;
using InstaCrud.Interfaces;
using InstaCrud.Core.Querying;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Handler;

public sealed class EntityCommandFactory : IEntityCommandFactory {
    private readonly IEntityRegistry _registry;

    public EntityCommandFactory(IEntityRegistry registry) {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public CrudCommandModel CreateInsert<TEntity>(TEntity entity)
        where TEntity : class {
        ArgumentNullException.ThrowIfNull(entity);
        CrudEntityDefinition definition = _registry.Get(typeof(TEntity));

        return new CrudCommandModel {
            OperationType = CrudOperationType.Insert,
            TableName = definition.TableName,
            Fields = CreateFields(
                definition.Properties.Where(x =>
                    !x.Ignore &&
                    !x.IgnoreInsert &&
                    !x.IsDatabaseGenerated),
                entity),
            ReturningFields = definition.Properties
                .Where(x => !x.Ignore && x.IsKey && x.IsDatabaseGenerated)
                .Select(x => new CrudField {
                    ColumnName = x.ColumnName,
                    ParameterName = x.PropertyName,
                    ValueType = x.PropertyType,
                    Value = null
                })
                .ToArray()
        };
    }

    public CrudCommandModel CreateSelect<TEntity>()
        where TEntity : class =>
        CreateSelect(new CrudQuery<TEntity>());

    public CrudCommandModel CreateSelect<TEntity>(CrudQuery<TEntity> query)
        where TEntity : class {
        ArgumentNullException.ThrowIfNull(query);
        CrudEntityDefinition definition = _registry.Get(typeof(TEntity));
        IReadOnlyCollection<CrudPropertyDefinition> selectedProperties =
            query.SelectedProperties.Count == 0
                ? definition.Properties.Where(x => !x.Ignore).ToArray()
                : query.SelectedProperties
                    .Select(x => GetQueryableProperty(definition, x))
                    .ToArray();

        return new CrudCommandModel {
            OperationType = CrudOperationType.Select,
            TableName = definition.TableName,
            Fields = selectedProperties
                .Select(x => new CrudField {
                    ColumnName = x.ColumnName,
                    ParameterName = x.PropertyName,
                    Value = null
                })
                .ToArray(),
            Filters = query.Filters
                .Select(x => new CrudFilter {
                    ColumnName = GetQueryableProperty(definition, x.PropertyName).ColumnName,
                    Operator = x.Operator,
                    Value = x.Value
                })
                .ToArray(),
            Sorts = query.Sorts
                .Select(x => new SortDefinition {
                    ColumnName = GetQueryableProperty(definition, x.PropertyName).ColumnName,
                    Descending = x.Descending
                })
                .ToArray(),
            Pagination = query.Pagination
        };
    }

    public CrudCommandModel CreateUpdate<TEntity>(TEntity entity)
        where TEntity : class {
        ArgumentNullException.ThrowIfNull(entity);
        CrudEntityDefinition definition = GetDefinitionWithKey<TEntity>();

        return new CrudCommandModel {
            OperationType = CrudOperationType.Update,
            TableName = definition.TableName,
            Fields = CreateFields(
                definition.Properties.Where(x =>
                    !x.Ignore &&
                    !x.IgnoreUpdate &&
                    !x.IsKey &&
                    !x.IsDatabaseGenerated),
                entity),
            Filters = CreateKeyFilters(definition, entity)
        };
    }

    public CrudCommandModel CreatePatch<TEntity>(
        TEntity entity,
        IReadOnlyCollection<string> propertyNames)
        where TEntity : class {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(propertyNames);

        CrudEntityDefinition definition = GetDefinitionWithKey<TEntity>();
        string[] requestedProperties = propertyNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedProperties.Length == 0)
            throw new ArgumentException("O patch exige pelo menos uma propriedade.", nameof(propertyNames));

        var propertiesByName = definition.Properties.ToDictionary(
            x => x.PropertyName,
            StringComparer.OrdinalIgnoreCase);
        var selectedProperties = new List<CrudPropertyDefinition>(requestedProperties.Length);

        foreach (string propertyName in requestedProperties) {
            if (!propertiesByName.TryGetValue(propertyName, out CrudPropertyDefinition? property)) {
                throw new ArgumentException(
                    $"A propriedade '{propertyName}' não pertence à entidade '{typeof(TEntity).Name}'.",
                    nameof(propertyNames));
            }

            if (property.Ignore || property.IgnorePatch || property.IsKey || property.IsDatabaseGenerated) {
                throw new ArgumentException(
                    $"A propriedade '{propertyName}' não pode ser alterada por patch.",
                    nameof(propertyNames));
            }

            selectedProperties.Add(property);
        }

        return new CrudCommandModel {
            OperationType = CrudOperationType.Patch,
            TableName = definition.TableName,
            Fields = CreateFields(selectedProperties, entity),
            Filters = CreateKeyFilters(definition, entity)
        };
    }

    public CrudCommandModel CreateDelete<TEntity>(TEntity entity)
        where TEntity : class {
        ArgumentNullException.ThrowIfNull(entity);
        CrudEntityDefinition definition = GetDefinitionWithKey<TEntity>();

        return new CrudCommandModel {
            OperationType = CrudOperationType.Delete,
            TableName = definition.TableName,
            Filters = CreateKeyFilters(definition, entity)
        };
    }

    private CrudEntityDefinition GetDefinitionWithKey<TEntity>()
        where TEntity : class {
        CrudEntityDefinition definition = _registry.Get(typeof(TEntity));

        if (!definition.HasKey) {
            throw new InvalidOperationException(
                $"A entidade '{typeof(TEntity).Name}' precisa de ao menos uma propriedade [Key] para esta operação.");
        }

        return definition;
    }

    private static IReadOnlyCollection<CrudField> CreateFields<TEntity>(
        IEnumerable<CrudPropertyDefinition> properties,
        TEntity entity)
        where TEntity : class =>
        properties
            .Select(x => new CrudField {
                ColumnName = x.ColumnName,
                ParameterName = x.PropertyName,
                Value = ReadValue(x, entity)
            })
            .ToArray();

    private static IReadOnlyCollection<CrudFilter> CreateKeyFilters<TEntity>(
        CrudEntityDefinition definition,
        TEntity entity)
        where TEntity : class =>
        definition.KeyProperties
            .Select(x => new CrudFilter {
                ColumnName = x.ColumnName,
                Operator = CrudFilterOperator.Equal,
                Value = ReadValue(x, entity)
            })
            .ToArray();

    private static object? ReadValue<TEntity>(
        CrudPropertyDefinition property,
        TEntity entity)
        where TEntity : class {
        if (property.Getter is null) {
            throw new InvalidOperationException(
                $"A propriedade '{property.PropertyName}' precisa ser legível para gerar um comando CRUD.");
        }

        return property.Getter(entity);
    }

    private static CrudPropertyDefinition GetQueryableProperty(
        CrudEntityDefinition definition,
        string propertyName) {
        CrudPropertyDefinition? property = definition.Properties.FirstOrDefault(x =>
            string.Equals(x.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase));

        if (property is null) {
            throw new ArgumentException(
                $"A propriedade '{propertyName}' não pertence à entidade '{definition.EntityType.Name}'.",
                nameof(propertyName));
        }

        if (property.Ignore) {
            throw new ArgumentException(
                $"A propriedade '{propertyName}' está ignorada e não pode ser usada em consultas.",
                nameof(propertyName));
        }

        return property;
    }
}
