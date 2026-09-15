using System.Text.RegularExpressions;

namespace InstaCrud.Sql;

public abstract partial class SqlDialect : ISqlDialect {
    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPartPattern();

    public abstract string Name { get; }

    public abstract string StatementTerminator { get; }

    protected abstract string ParameterPrefix { get; }

    public string Identifier(string identifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        string[] parts = identifier.Split('.');

        if (parts.Any(x => !IdentifierPartPattern().IsMatch(x))) {
            throw new ArgumentException(
                $"O identificador SQL '{identifier}' é inválido.",
                nameof(identifier));
        }

        return string.Join('.', parts.Select(QuoteIdentifierPart));
    }

    public string Parameter(string parameterName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

        if (!IdentifierPartPattern().IsMatch(parameterName)) {
            throw new ArgumentException(
                $"O parâmetro SQL '{parameterName}' é inválido.",
                nameof(parameterName));
        }

        return $"{ParameterPrefix}{parameterName}";
    }

    public abstract string Pagination(string offsetParameter, string pageSizeParameter);

    public abstract SqlInsertReturningDefinition InsertReturning(
        IReadOnlyCollection<SqlReturningField> fields);

    protected abstract string QuoteIdentifierPart(string identifierPart);
}
