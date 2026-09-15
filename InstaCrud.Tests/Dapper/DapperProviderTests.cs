using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Abstractions.Attributes;
using InstaCrud.Dapper;

namespace InstaCrud.Tests.Dapper;

[TestClass]
public sealed class DapperProviderTests {
    private readonly DapperProvider _provider = new();

    [Crud]
    [Table("CLIENTE")]
    private sealed class Cliente {
        [Key]
        [DatabaseGenerated]
        public int Id { get; set; }

        [Column("NOME_COMPLETO")]
        public string Nome { get; set; } = string.Empty;
    }

    [TestMethod]
    public void Deve_Gerar_Comando_Diretamente_Da_Entidade() {
        var crud = DapperCrud.Create<Cliente>();

        var result = crud.Insert(new Cliente { Nome = "Ana" });

        Assert.AreEqual(
            "INSERT INTO [CLIENTE] ([NOME_COMPLETO]) OUTPUT INSERTED.[Id] VALUES (@p0);",
            result.Sql);
        Assert.AreEqual("Ana", result.Parameters["p0"]);
    }

    [TestMethod]
    public void Deve_Criar_Alias_De_Coluna_Para_Materializacao() {
        var crud = DapperCrud.Create<Cliente>();

        var result = crud.Select<Cliente>();

        Assert.AreEqual(
            "SELECT [Id], [NOME_COMPLETO] AS [Nome] FROM [CLIENTE];",
            result.Sql);
    }

    [TestMethod]
    public void Deve_Gerar_Insert_Parametrizado() {
        var command = Command(
            CrudOperationType.Insert,
            fields: [Field("NOME", "Maria"), Field("ATIVO", true)]);

        var result = _provider.Build(command);

        Assert.AreEqual(
            "INSERT INTO [dbo].[USUARIO] ([NOME], [ATIVO]) VALUES (@p0, @p1);",
            result.Sql);
        Assert.AreEqual("Maria", result.Parameters["p0"]);
        Assert.IsTrue((bool)result.Parameters["p1"]!);
    }

    [TestMethod]
    public void Deve_Gerar_Select_Com_Filtros_Ordenacao_E_Paginacao() {
        var command = Command(
            CrudOperationType.Select,
            fields: [Field("ID"), Field("NOME")],
            filters: [Filter("ID", CrudFilterOperator.In, new[] { 10, 20 })],
            sorts: [new SortDefinition { ColumnName = "NOME" }],
            pagination: new PaginationDefinition { Page = 2, PageSize = 25 });

        var result = _provider.Build(command);

        Assert.AreEqual(
            "SELECT [ID], [NOME] FROM [dbo].[USUARIO] WHERE [ID] IN (@p0, @p1) ORDER BY [NOME] ASC OFFSET @p2 ROWS FETCH NEXT @p3 ROWS ONLY;",
            result.Sql);
        CollectionAssert.AreEqual(
            new object?[] { 10, 20, 25, 25 },
            result.Parameters.Values.ToArray());
    }

    [TestMethod]
    public void Deve_Gerar_Update_Com_Filtro() {
        var command = Command(
            CrudOperationType.Update,
            fields: [Field("NOME", "João")],
            filters: [Filter("ID", CrudFilterOperator.Equal, 7)]);

        var result = _provider.Build(command);

        Assert.AreEqual(
            "UPDATE [dbo].[USUARIO] SET [NOME] = @p0 WHERE [ID] = @p1;",
            result.Sql);
    }

    [TestMethod]
    public void Deve_Gerar_Patch_Com_Filtro() {
        var command = Command(
            CrudOperationType.Patch,
            fields: [Field("ATIVO", false)],
            filters: [Filter("ID", CrudFilterOperator.Equal, 7)]);

        var result = _provider.Build(command);

        Assert.AreEqual(
            "UPDATE [dbo].[USUARIO] SET [ATIVO] = @p0 WHERE [ID] = @p1;",
            result.Sql);
    }

    [TestMethod]
    public void Deve_Gerar_Delete_Com_Filtro() {
        var command = Command(
            CrudOperationType.Delete,
            filters: [Filter("ID", CrudFilterOperator.Equal, 7)]);

        var result = _provider.Build(command);

        Assert.AreEqual("DELETE FROM [dbo].[USUARIO] WHERE [ID] = @p0;", result.Sql);
    }

    [TestMethod]
    public void Deve_Traduzir_Comparacao_Com_Nulo() {
        var command = Command(
            CrudOperationType.Select,
            filters: [Filter("EXCLUIDO_EM", CrudFilterOperator.Equal, null)]);

        var result = _provider.Build(command);

        Assert.AreEqual(
            "SELECT * FROM [dbo].[USUARIO] WHERE [EXCLUIDO_EM] IS NULL;",
            result.Sql);
        Assert.IsEmpty(result.Parameters);
    }

    [TestMethod]
    public void Deve_Impedir_Update_Sem_Filtro() {
        var command = Command(
            CrudOperationType.Update,
            fields: [Field("ATIVO", false)]);

        Assert.ThrowsExactly<ArgumentException>(() => _provider.Build(command));
    }

    [TestMethod]
    public void Deve_Impedir_Delete_Sem_Filtro() {
        var command = Command(CrudOperationType.Delete);

        Assert.ThrowsExactly<ArgumentException>(() => _provider.Build(command));
    }

    [TestMethod]
    public void Deve_Rejeitar_Identificador_Perigoso() {
        var command = new CrudCommand {
            OperationType = CrudOperationType.Select,
            TableName = "USUARIO; DROP TABLE USUARIO"
        };

        Assert.ThrowsExactly<ArgumentException>(() => _provider.Build(command));
    }

    private static CrudCommand Command(
        CrudOperationType operation,
        IReadOnlyCollection<CrudField>? fields = null,
        IReadOnlyCollection<CrudFilter>? filters = null,
        IReadOnlyCollection<SortDefinition>? sorts = null,
        PaginationDefinition? pagination = null) => new() {
        OperationType = operation,
        TableName = "dbo.USUARIO",
        Fields = fields ?? [],
        Filters = filters ?? [],
        Sorts = sorts ?? [],
        Pagination = pagination
    };

    private static CrudField Field(string columnName, object? value = null) => new() {
        ColumnName = columnName,
        Value = value
    };

    private static CrudFilter Filter(
        string columnName,
        CrudFilterOperator filterOperator,
        object? value) => new() {
        ColumnName = columnName,
        Operator = filterOperator,
        Value = value
    };
}
