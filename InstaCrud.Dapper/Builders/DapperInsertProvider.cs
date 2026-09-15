using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Interfaces;
using InstaCrud.Abstractions.Sql;
using CrudCommandModel = InstaCrud.Abstractions.CrudCommand.CrudCommand;

namespace InstaCrud.Dapper.Builders;

public sealed class DapperInsertProvider : ICrudInsertProvider<SqlCommandDefinition> {
    public SqlCommandDefinition Build(CrudCommandModel command) {
        DapperSql.ValidateOperation(command, CrudOperationType.Insert);

        if (command.Fields.Count == 0)
            throw new ArgumentException("O insert exige pelo menos um campo.", nameof(command));

        DapperSql.ValidateDistinctFields(command.Fields);

        var parameters = new Dictionary<string, object?>();
        int parameterIndex = 0;
        string columns = string.Join(", ", command.Fields.Select(x => DapperSql.Identifier(x.ColumnName)));
        string values = string.Join(
            ", ",
            command.Fields.Select(x => DapperSql.AddParameter(parameters, x.Value, ref parameterIndex)));

        return new SqlCommandDefinition {
            Sql = $"INSERT INTO {DapperSql.Identifier(command.TableName)} ({columns}) VALUES ({values});",
            Parameters = parameters
        };
    }
}
