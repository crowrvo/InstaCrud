using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Abstractions.Sql;
using InstaCrud.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper.Builders;

public sealed class DapperInsertProvider : ICrudInsertProvider<SqlCommandDefinition> {
    private readonly DapperSql _sql;

    public DapperInsertProvider(ISqlDialect dialect) {
        _sql = new DapperSql(dialect);
    }

    public SqlCommandDefinition Build(CrudCommandModel command) {
        _sql.ValidateOperation(command, CrudOperationType.Insert);

        if (command.Fields.Count == 0)
            throw new ArgumentException("O insert exige pelo menos um campo.", nameof(command));

        DapperSql.ValidateDistinctFields(command.Fields);

        var parameters = new Dictionary<string, object?>();
        int parameterIndex = 0;
        string columns = string.Join(", ", command.Fields.Select(x => _sql.Identifier(x.ColumnName)));
        string values = string.Join(
            ", ",
            command.Fields.Select(x => _sql.AddParameter(parameters, x.Value, ref parameterIndex)));
        SqlInsertReturningDefinition returning = _sql.InsertReturning(command.ReturningFields);

        return new SqlCommandDefinition {
            Sql = $"INSERT INTO {_sql.Identifier(command.TableName)} ({columns}){returning.BeforeValues} VALUES ({values}){returning.AfterValues}{_sql.StatementTerminator}",
            Parameters = parameters,
            ResultMode = returning.ResultMode,
            OutputParameters = returning.OutputParameters
        };
    }
}
