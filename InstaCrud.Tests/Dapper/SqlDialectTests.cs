using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Dapper;
using InstaCrud.Sql;

namespace InstaCrud.Tests.Dapper;

[TestClass]
public sealed class SqlDialectTests {
    [TestMethod]
    public void Deve_Manter_SqlServer_Como_Dialeto_Padrao() {
        var provider = new DapperProvider();

        var result = provider.Build(SelectCommand());

        Assert.AreEqual(
            "SELECT * FROM [APP].[USUARIO] WHERE [ATIVO] = @p0 ORDER BY [NOME] ASC OFFSET @p1 ROWS FETCH NEXT @p2 ROWS ONLY;",
            result.Sql);
    }

    [TestMethod]
    public void Deve_Gerar_Select_Oracle() {
        var provider = new DapperProvider(OracleDialect.Instance);

        var result = provider.Build(SelectCommand());

        Assert.AreEqual(
            "SELECT * FROM \"APP\".\"USUARIO\" WHERE \"ATIVO\" = :p0 ORDER BY \"NOME\" ASC OFFSET :p1 ROWS FETCH NEXT :p2 ROWS ONLY",
            result.Sql);
        CollectionAssert.AreEqual(
            new object?[] { true, 20, 20 },
            result.Parameters.Values.ToArray());
    }

    [TestMethod]
    public void Deve_Gerar_Insert_Oracle_Sem_Chave_Gerada() {
        var provider = new DapperProvider(OracleDialect.Instance);
        var command = new CrudCommand {
            OperationType = CrudOperationType.Insert,
            TableName = "APP.USUARIO",
            Fields = [new CrudField {
                ColumnName = "NOME",
                Value = "Maria"
            }]
        };

        var result = provider.Build(command);

        Assert.AreEqual(
            "INSERT INTO \"APP\".\"USUARIO\" (\"NOME\") VALUES (:p0)",
            result.Sql);
    }

    [TestMethod]
    public void Deve_Explicitar_Limitacao_De_Returning_No_Oracle() {
        var provider = new DapperProvider(OracleDialect.Instance);
        var command = new CrudCommand {
            OperationType = CrudOperationType.Insert,
            TableName = "APP.USUARIO",
            Fields = [new CrudField {
                ColumnName = "NOME",
                Value = "Maria"
            }],
            ReturningFields = [new CrudField {
                ColumnName = "ID",
                ParameterName = "Id",
                Value = null
            }]
        };

        Assert.ThrowsExactly<NotSupportedException>(() => provider.Build(command));
    }

    private static CrudCommand SelectCommand() => new() {
        OperationType = CrudOperationType.Select,
        TableName = "APP.USUARIO",
        Filters = [new CrudFilter {
            ColumnName = "ATIVO",
            Operator = CrudFilterOperator.Equal,
            Value = true
        }],
        Sorts = [new SortDefinition { ColumnName = "NOME" }],
        Pagination = new PaginationDefinition { Page = 2, PageSize = 20 }
    };
}
