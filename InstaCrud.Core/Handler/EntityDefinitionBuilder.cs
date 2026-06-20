namespace InstaCrud.Handler;

using InstaCrud.Core;
using InstaCrud.Decorators;
using InstaCrud.Enums;
using InstaCrud.Interfaces;
using System.Linq.Expressions;
using System.Reflection;

public sealed class EntityDefinitionBuilder
    : IEntityDefinitionBuilder {
    public CrudEntityDefinition Build(Type entityType) {
        ValidarCrudAttribute(entityType);

        CrudAttribute crudAttribute =
            entityType.GetCustomAttribute<CrudAttribute>()!;

        string routeName =
            crudAttribute.RouteName ??
            entityType.Name;

        string tableName =
            entityType.GetCustomAttribute<TableAttribute>()?.Name
            ?? entityType.Name;

        List<CrudPropertyDefinition> properties =
            ObterPropriedades(entityType);

        List<CrudPropertyDefinition> keyProperties =
            properties
                .Where(x => x.IsKey)
                .ToList();

        return new CrudEntityDefinition {
            EntityType = entityType,
            RouteName = routeName,
            TableName = tableName,
            Properties = properties,
            KeyProperties = keyProperties
        };
    }

    private static void ValidarCrudAttribute(Type entityType) {
        if (!entityType.IsDefined(typeof(CrudAttribute), false)) {
            throw new InvalidOperationException(
                $"A entidade '{entityType.Name}' não possui o atributo [Crud].");
        }
    }

    private static List<CrudPropertyDefinition> ObterPropriedades(Type entityType) {
        return entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(CriarPropriedade)
            .ToList();
    }

    private static CrudPropertyDefinition CriarPropriedade(PropertyInfo property) {
        CrudPropertyFlags flags = ObterFlags(property);

        return new CrudPropertyDefinition {
            PropertyInfo = property,

            PropertyName = property.Name,

            ColumnName =
                property.GetCustomAttribute<ColumnAttribute>()?.Name
                ?? property.Name,

            PropertyType = property.PropertyType,

            Flags = flags,

            Getter = CriarGetter(property),

            Setter = CriarSetter(property)
        };
    }

    private static CrudPropertyFlags ObterFlags(PropertyInfo property) {
        CrudPropertyFlags flags = CrudPropertyFlags.None;

        if (property.IsDefined(typeof(KeyAttribute), false))
            flags |= CrudPropertyFlags.Key;

        if (property.IsDefined(typeof(DatabaseGeneratedAttribute), false))
            flags |= CrudPropertyFlags.DatabaseGenerated;

        if (property.IsDefined(typeof(IgnoreAttribute), false))
            flags |= CrudPropertyFlags.Ignore;

        if (property.IsDefined(typeof(IgnoreInsertAttribute), false))
            flags |= CrudPropertyFlags.IgnoreInsert;

        if (property.IsDefined(typeof(IgnoreUpdateAttribute), false))
            flags |= CrudPropertyFlags.IgnoreUpdate;


        return flags;
    }

    private static Func<object, object?> CriarGetter(PropertyInfo property) {
        ParameterExpression instance =
            Expression.Parameter(typeof(object));

        UnaryExpression cast =
            Expression.Convert(instance, property.DeclaringType!);

        MemberExpression access =
            Expression.Property(cast, property);

        UnaryExpression convert =
            Expression.Convert(access, typeof(object));

        return Expression
            .Lambda<Func<object, object?>>(
                convert,
                instance)
            .Compile();
    }

    private static Action<object, object?>? CriarSetter(PropertyInfo property) {
        if (!property.CanWrite)
            return null;

        ParameterExpression instance =
            Expression.Parameter(typeof(object));

        ParameterExpression value =
            Expression.Parameter(typeof(object));

        UnaryExpression castInstance =
            Expression.Convert(instance, property.DeclaringType!);

        UnaryExpression castValue =
            Expression.Convert(value, property.PropertyType);

        BinaryExpression assign =
            Expression.Assign(
                Expression.Property(castInstance, property),
                castValue);

        return Expression
            .Lambda<Action<object, object?>>(
                assign,
                instance,
                value)
            .Compile();
    }
}
