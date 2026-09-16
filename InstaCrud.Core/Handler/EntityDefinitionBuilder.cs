namespace InstaCrud.Handler;

using InstaCrud.Abstractions.Attributes;
using InstaCrud.Core;
using InstaCrud.Enums;
using InstaCrud.Exceptions;
using InstaCrud.Interfaces;
using System.Linq.Expressions;
using System.Reflection;

public sealed class EntityDefinitionBuilder
    : IEntityDefinitionBuilder {
    public CrudEntityDefinition Build(Type entityType) {
        ArgumentNullException.ThrowIfNull(entityType);
        ValidarTipo(entityType);
        ValidarCrudAttribute(entityType);

        CrudAttribute crudAttribute =
            entityType.GetCustomAttribute<CrudAttribute>()!;

        string routeName =
            crudAttribute.RouteName ??
            entityType.Name;

        string tableName =
            entityType.GetCustomAttribute<TableAttribute>()?.Name
            ?? entityType.Name;

        ValidarNome(routeName, "rota", entityType);
        ValidarNome(tableName, "tabela", entityType);

        List<CrudPropertyDefinition> properties =
            ObterPropriedades(entityType);

        List<CrudPropertyDefinition> keyProperties =
            properties
                .Where(x => x.IsKey)
                .ToList();

        ValidarPropriedades(entityType, properties);

        return new CrudEntityDefinition {
            EntityType = entityType,
            RouteName = routeName,
            TableName = tableName,
            Properties = properties,
            KeyProperties = keyProperties
        };
    }

    private static void ValidarTipo(Type entityType) {
        if (!entityType.IsClass || entityType.IsAbstract || entityType.ContainsGenericParameters) {
            throw new CrudConfigurationException(
                $"O tipo '{entityType.Name}' deve ser uma classe concreta e fechada para ser uma entidade CRUD.",
                entityType);
        }
    }

    private static void ValidarCrudAttribute(Type entityType) {
        if (!entityType.IsDefined(typeof(CrudAttribute), false)) {
            throw new CrudConfigurationException(
                $"A entidade '{entityType.Name}' não possui o atributo [Crud].",
                entityType);
        }
    }

    private static void ValidarNome(
        string? name,
        string kind,
        Type entityType) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new CrudConfigurationException(
                $"O nome de {kind} da entidade '{entityType.Name}' não pode ser vazio.",
                entityType);
        }
    }

    private static void ValidarPropriedades(
        Type entityType,
        IReadOnlyCollection<CrudPropertyDefinition> properties) {
        foreach (CrudPropertyDefinition property in properties) {
            ValidarNomeDaColuna(entityType, property);

            if (property.IsKey && property.Ignore) {
                throw new CrudConfigurationException(
                    $"A propriedade chave '{property.PropertyName}' da entidade '{entityType.Name}' não pode usar [Ignore].",
                    entityType,
                    property.PropertyName);
            }

            if (property.Ignore)
                continue;

            if (property.Getter is null) {
                throw new CrudConfigurationException(
                    $"A propriedade '{property.PropertyName}' da entidade '{entityType.Name}' precisa ser legível.",
                    entityType,
                    property.PropertyName);
            }

            if (property.IsDatabaseGenerated && property.Setter is null) {
                throw new CrudConfigurationException(
                    $"A propriedade gerada '{property.PropertyName}' da entidade '{entityType.Name}' precisa ser gravável.",
                    entityType,
                    property.PropertyName);
            }
        }

        string? duplicateColumn = properties
            .Where(x => !x.Ignore)
            .GroupBy(x => x.ColumnName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1)
            ?.Key;

        if (duplicateColumn is not null) {
            throw new CrudConfigurationException(
                $"A coluna '{duplicateColumn}' está mapeada por mais de uma propriedade da entidade '{entityType.Name}'.",
                entityType);
        }
    }

    private static void ValidarNomeDaColuna(
        Type entityType,
        CrudPropertyDefinition property) {
        if (string.IsNullOrWhiteSpace(property.ColumnName)) {
            throw new CrudConfigurationException(
                $"O nome da coluna da propriedade '{property.PropertyName}' não pode ser vazio.",
                entityType,
                property.PropertyName);
        }
    }

    private static List<CrudPropertyDefinition> ObterPropriedades(Type entityType) {
        return entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.GetIndexParameters().Length == 0)
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

    private static Func<object, object?>? CriarGetter(PropertyInfo property) {
        if (!property.CanRead)
            return null;

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
