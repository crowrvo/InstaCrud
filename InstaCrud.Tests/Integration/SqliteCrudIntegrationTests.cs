using InstaCrud.Abstractions.Attributes;
using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Core.Querying;
using InstaCrud.Dapper;
using InstaCrud.Sql;
using Microsoft.Data.Sqlite;

namespace InstaCrud.Tests.Integration;

[TestClass]
public sealed class SqliteCrudIntegrationTests {
    [Crud("usuarios")]
    [Table("USUARIO")]
    private sealed class Usuario {
        [Key]
        [DatabaseGenerated]
        [Column("ID")]
        public long Id { get; set; }

        [Column("NOME")]
        public string Nome { get; set; } = string.Empty;

        [Column("EMAIL")]
        public string Email { get; set; } = string.Empty;

        [Column("ATIVO")]
        public bool Ativo { get; set; }
    }

    [TestMethod]
    public async Task Deve_Executar_Ciclo_Crud_Completo() {
        await using SqliteConnection connection = await CreateConnectionAsync();
        DapperCrudExecutor crud = DapperCrud.CreateExecutor<Usuario>(
            connection,
            SqliteDialect.Instance);
        var maria = new Usuario {
            Nome = "Maria",
            Email = "maria@example.com",
            Ativo = true
        };
        var joao = new Usuario {
            Nome = "João",
            Email = "joao@example.com",
            Ativo = true
        };

        Assert.AreEqual(1, await crud.InsertAsync(maria));
        Assert.AreEqual(1, await crud.InsertAsync(joao));
        Assert.IsGreaterThan(0L, maria.Id);
        Assert.IsGreaterThan(maria.Id, joao.Id);

        Usuario? encontrado = await crud.FindAsync<Usuario>(maria.Id);
        Assert.IsNotNull(encontrado);
        Assert.AreEqual("Maria", encontrado.Nome);

        var query = new CrudQuery<Usuario>()
            .Where(x => x.Ativo, CrudFilterOperator.Equal, true)
            .OrderBy(x => x.Nome)
            .Page(1, 1);
        PagedResult<Usuario> pagina = await crud.PageAsync(query);

        Assert.AreEqual(2L, pagina.Total);
        Assert.AreEqual(2L, pagina.TotalPages);
        Assert.HasCount(1, pagina.Items);

        maria.Nome = "Maria Silva";
        Assert.AreEqual(1, await crud.UpdateAsync(maria));

        maria.Ativo = false;
        Assert.AreEqual(1, await crud.PatchAsync(maria, [nameof(Usuario.Ativo)]));
        encontrado = await crud.FindAsync<Usuario>(maria.Id);
        Assert.IsNotNull(encontrado);
        Assert.AreEqual("Maria Silva", encontrado.Nome);
        Assert.IsFalse(encontrado.Ativo);

        Assert.AreEqual(1, await crud.DeleteAsync(joao));
        Assert.AreEqual(1L, await crud.CountAsync<Usuario>());
    }

    [TestMethod]
    public async Task Deve_Respeitar_Rollback_Da_Transacao() {
        await using SqliteConnection connection = await CreateConnectionAsync();
        DapperCrudExecutor crud = DapperCrud.CreateExecutor<Usuario>(
            connection,
            SqliteDialect.Instance);
        await using SqliteTransaction transaction = connection.BeginTransaction();
        var usuario = new Usuario {
            Nome = "Temporário",
            Email = "temp@example.com",
            Ativo = true
        };

        await crud.InsertAsync(usuario, transaction);
        await transaction.RollbackAsync();

        Assert.AreEqual(0L, await crud.CountAsync<Usuario>());
    }

    private static async Task<SqliteConnection> CreateConnectionAsync() {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE USUARIO (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                NOME TEXT NOT NULL,
                EMAIL TEXT NOT NULL,
                ATIVO INTEGER NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();

        return connection;
    }
}
