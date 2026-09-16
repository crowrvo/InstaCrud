using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Core.Querying;
using InstaCrud.Dapper;
using InstaCrud.Sql;
using InstantCrud.Sample;
using Microsoft.Data.Sqlite;

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

await CreateSchemaAsync(connection);

DapperCrudExecutor crud = DapperCrud.CreateExecutor<Usuario>(
    connection,
    SqliteDialect.Instance);

var maria = new Usuario {
    Nome = "Maria",
    Email = "maria@example.com",
    Ativo = true,
    CriadoEm = DateTime.UtcNow
};
var joao = new Usuario {
    Nome = "João",
    Email = "joao@example.com",
    Ativo = true,
    CriadoEm = DateTime.UtcNow
};

await crud.InsertAsync(maria);
await crud.InsertAsync(joao);
Console.WriteLine($"Inseridos: Maria #{maria.Id}, João #{joao.Id}");

Usuario? encontrado = await crud.FindAsync<Usuario>(maria.Id);
Console.WriteLine($"Encontrado: {encontrado?.Nome} ({encontrado?.Email})");

var query = new CrudQuery<Usuario>()
    .Where(x => x.Ativo, CrudFilterOperator.Equal, true)
    .OrderBy(x => x.Nome)
    .Page(1, 10);

PagedResult<Usuario> pagina = await crud.PageAsync(query);
Console.WriteLine($"Ativos: {pagina.Total}; páginas: {pagina.TotalPages}");

maria.Nome = "Maria Silva";
await crud.UpdateAsync(maria);

maria.Ativo = false;
await crud.PatchAsync(maria, [nameof(Usuario.Ativo)]);

await crud.DeleteAsync(joao);
long restantes = await crud.CountAsync<Usuario>();
Console.WriteLine($"Restantes após update, patch e delete: {restantes}");

static async Task CreateSchemaAsync(SqliteConnection connection) {
    await using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE USUARIO (
            ID INTEGER PRIMARY KEY AUTOINCREMENT,
            NOME TEXT NOT NULL,
            EMAIL TEXT NOT NULL,
            ATIVO INTEGER NOT NULL,
            CRIADO_EM TEXT NOT NULL
        );
        """;
    await command.ExecuteNonQueryAsync();
}
