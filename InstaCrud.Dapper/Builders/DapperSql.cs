using System.Collections;
using System.Text.RegularExpressions;
using InstaCrud.Abstractions.CrudCommand;

namespace InstaCrud.Dapper.Builders;

internal static partial class DapperSql {
    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPartPattern();

    public static string Identifier(string identifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        string[] parts = identifier.Split('.');

        if (parts.Any(x => !IdentifierPartPattern().IsMatch(x))) {
            throw new ArgumentException(
                $"O identificador SQL '{identifier}' é inválido.",
                nameof(identifier));
        }

        return string.Join('.', parts.Select(x => $"[{x}]"));
    }

    public static void ValidateOperation(
        CrudCommand command,
        CrudOperationType expectedOperation) {
        ArgumentNullException.ThrowIfNull(command);

        if (command.OperationType != expectedOperation) {
            throw new ArgumentException(
                $"O comando '{command.OperationType}' não pode ser processado como '{expectedOperation}'.",
                nameof(command));
        }

        Identifier(command.TableName);
    }

    public static void ValidateDistinctFields(IReadOnlyCollection<CrudField> fields) {
        string? duplicate = fields
            .GroupBy(x => x.ColumnName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1)
            ?.Key;

        if (duplicate is not null) {
            throw new ArgumentException(
                $"A coluna '{duplicate}' foi informada mais de uma vez.",
                nameof(fields));
        }
    }

    public static string AddParameter(
        IDictionary<string, object?> parameters,
        object? value,
        ref int parameterIndex) {
        string name = $"p{parameterIndex++}";
        parameters.Add(name, value);
        return $"@{name}";
    }

    public static string Where(
        IReadOnlyCollection<CrudFilter> filters,
        IDictionary<string, object?> parameters,
        ref int parameterIndex) {
        if (filters.Count == 0)
            return string.Empty;

        var expressions = new List<string>(filters.Count);

        foreach (CrudFilter filter in filters) {
            string column = Identifier(filter.ColumnName);

            if (filter.Operator is CrudFilterOperator.IsNull) {
                expressions.Add($"{column} IS NULL");
                continue;
            }

            if (filter.Operator is CrudFilterOperator.IsNotNull) {
                expressions.Add($"{column} IS NOT NULL");
                continue;
            }

            if (filter.Value is null) {
                expressions.Add(filter.Operator switch {
                    CrudFilterOperator.Equal => $"{column} IS NULL",
                    CrudFilterOperator.NotEqual => $"{column} IS NOT NULL",
                    _ => throw new ArgumentException(
                        $"O operador '{filter.Operator}' não aceita valor nulo.",
                        nameof(filters))
                });
                continue;
            }

            if (filter.Operator is CrudFilterOperator.In) {
                expressions.Add(BuildInExpression(
                    column,
                    filter.Value,
                    parameters,
                    ref parameterIndex));
                continue;
            }

            string sqlOperator = filter.Operator switch {
                CrudFilterOperator.Equal => "=",
                CrudFilterOperator.NotEqual => "<>",
                CrudFilterOperator.GreaterThan => ">",
                CrudFilterOperator.GreaterThanOrEqual => ">=",
                CrudFilterOperator.LessThan => "<",
                CrudFilterOperator.LessThanOrEqual => "<=",
                CrudFilterOperator.Like => "LIKE",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(filters),
                    filter.Operator,
                    "Operador de filtro desconhecido.")
            };
            string parameter = AddParameter(parameters, filter.Value, ref parameterIndex);
            expressions.Add($"{column} {sqlOperator} {parameter}");
        }

        return $" WHERE {string.Join(" AND ", expressions)}";
    }

    public static string OrderBy(IReadOnlyCollection<SortDefinition> sorts) {
        if (sorts.Count == 0)
            return string.Empty;

        return " ORDER BY " + string.Join(
            ", ",
            sorts.Select(x => $"{Identifier(x.ColumnName)}{(x.Descending ? " DESC" : " ASC")}"));
    }

    public static string Pagination(
        PaginationDefinition? pagination,
        IReadOnlyCollection<SortDefinition> sorts,
        IDictionary<string, object?> parameters,
        ref int parameterIndex) {
        if (pagination is null)
            return string.Empty;

        if (pagination.Page < 1)
            throw new ArgumentOutOfRangeException(nameof(pagination), "A página deve ser maior que zero.");

        if (pagination.PageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pagination), "O tamanho da página deve ser maior que zero.");

        if (sorts.Count == 0) {
            throw new ArgumentException(
                "A paginação exige pelo menos uma ordenação para produzir resultados determinísticos.",
                nameof(sorts));
        }

        int offset = checked((pagination.Page - 1) * pagination.PageSize);
        string offsetParameter = AddParameter(parameters, offset, ref parameterIndex);
        string pageSizeParameter = AddParameter(parameters, pagination.PageSize, ref parameterIndex);

        return $" OFFSET {offsetParameter} ROWS FETCH NEXT {pageSizeParameter} ROWS ONLY";
    }

    private static string BuildInExpression(
        string column,
        object value,
        IDictionary<string, object?> parameters,
        ref int parameterIndex) {
        if (value is string || value is not IEnumerable values) {
            throw new ArgumentException(
                "O operador 'In' exige uma coleção de valores.",
                nameof(value));
        }

        var parameterNames = new List<string>();

        foreach (object? item in values) {
            parameterNames.Add(AddParameter(parameters, item, ref parameterIndex));
        }

        if (parameterNames.Count == 0)
            throw new ArgumentException("O operador 'In' não aceita uma coleção vazia.", nameof(value));

        return $"{column} IN ({string.Join(", ", parameterNames)})";
    }
}
