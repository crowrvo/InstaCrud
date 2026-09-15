using System.Linq.Expressions;
using System.Reflection;
using InstaCrud.Abstractions.CrudCommand;

namespace InstaCrud.Core.Querying;

public sealed class CrudQuery<TEntity>
    where TEntity : class {
    private readonly List<string> _selectedProperties = [];
    private readonly List<EntityQueryFilter> _filters = [];
    private readonly List<EntityQuerySort> _sorts = [];

    public IReadOnlyCollection<string> SelectedProperties => _selectedProperties.AsReadOnly();

    public IReadOnlyCollection<EntityQueryFilter> Filters => _filters.AsReadOnly();

    public IReadOnlyCollection<EntityQuerySort> Sorts => _sorts.AsReadOnly();

    public PaginationDefinition? Pagination { get; private set; }

    public CrudQuery<TEntity> Select(
        params Expression<Func<TEntity, object?>>[] properties) {
        ArgumentNullException.ThrowIfNull(properties);

        foreach (Expression<Func<TEntity, object?>> property in properties) {
            string propertyName = GetPropertyName(property);

            if (!_selectedProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
                _selectedProperties.Add(propertyName);
        }

        return this;
    }

    public CrudQuery<TEntity> Where<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        CrudFilterOperator filterOperator,
        object? value = null) {
        _filters.Add(new EntityQueryFilter(
            GetPropertyName(property),
            filterOperator,
            value));
        return this;
    }

    public CrudQuery<TEntity> OrderBy<TProperty>(
        Expression<Func<TEntity, TProperty>> property) {
        _sorts.Add(new EntityQuerySort(GetPropertyName(property), false));
        return this;
    }

    public CrudQuery<TEntity> OrderByDescending<TProperty>(
        Expression<Func<TEntity, TProperty>> property) {
        _sorts.Add(new EntityQuerySort(GetPropertyName(property), true));
        return this;
    }

    public CrudQuery<TEntity> Page(int page, int pageSize) {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "A página deve ser maior que zero.");

        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "O tamanho da página deve ser maior que zero.");

        Pagination = new PaginationDefinition {
            Page = page,
            PageSize = pageSize
        };
        return this;
    }

    private static string GetPropertyName<TProperty>(
        Expression<Func<TEntity, TProperty>> expression) {
        ArgumentNullException.ThrowIfNull(expression);

        Expression body = expression.Body is UnaryExpression {
            NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
        } conversion
            ? conversion.Operand
            : expression.Body;

        if (body is not MemberExpression {
            Expression: ParameterExpression,
            Member: PropertyInfo property
        }) {
            throw new ArgumentException(
                "A expressão deve acessar diretamente uma propriedade da entidade.",
                nameof(expression));
        }

        return property.Name;
    }
}
