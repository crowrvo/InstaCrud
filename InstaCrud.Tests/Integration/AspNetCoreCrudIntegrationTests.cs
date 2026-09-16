using System.Data;
using System.Net;
using System.Net.Http.Json;
using InstaCrud.Abstractions.Attributes;
using InstaCrud.AspNetCore;
using InstaCrud.Dapper;
using InstaCrud.Interfaces;
using InstaCrud.Sql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace InstaCrud.Tests.Integration;

[TestClass]
public sealed class AspNetCoreCrudIntegrationTests {
    [Crud("usuarios")]
    [Table("USUARIO")]
    private sealed class Usuario {
        [Key]
        [DatabaseGenerated]
        [Column("ID")]
        public long Id { get; set; }

        [Column("NOME")]
        public string Nome { get; set; } = string.Empty;

        [Column("ATIVO")]
        public bool Ativo { get; set; }
    }

    [TestMethod]
    public async Task Deve_Publicar_Crud_OpenApi_E_Scalar() {
        const string connectionString = "Data Source=InstantCrudApiTests;Mode=Memory;Cache=Shared";
        await using var keeper = new SqliteConnection(connectionString);
        await keeper.OpenAsync();
        await CreateSchemaAsync(keeper);

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));
        builder.Services.AddInstantCrud(options => options
            .AddEntity<Usuario>()
            .UseExecutor(CreateExecutor));

        await using WebApplication app = builder.Build();
        app.MapInstantCrud();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();

        HttpResponseMessage insertResponse = await client.PostAsJsonAsync(
            "/api/usuarios",
            new Usuario { Nome = "Maria", Ativo = true });
        Assert.AreEqual(HttpStatusCode.Created, insertResponse.StatusCode);
        Usuario? inserted = await insertResponse.Content.ReadFromJsonAsync<Usuario>();
        Assert.IsNotNull(inserted);
        Assert.IsGreaterThan(0L, inserted.Id);

        Usuario? found = await client.GetFromJsonAsync<Usuario>($"/api/usuarios/{inserted.Id}");
        Assert.IsNotNull(found);
        Assert.AreEqual("Maria", found.Nome);

        HttpResponseMessage pageResponse = await client.GetAsync("/api/usuarios?page=1&pageSize=10");
        Assert.AreEqual(HttpStatusCode.OK, pageResponse.StatusCode);

        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            $"/api/usuarios/{inserted.Id}",
            new Usuario { Nome = "Maria Silva", Ativo = true });
        Assert.AreEqual(HttpStatusCode.NoContent, updateResponse.StatusCode);

        HttpResponseMessage patchResponse = await client.PatchAsJsonAsync(
            $"/api/usuarios/{inserted.Id}",
            new { ativo = false });
        Assert.AreEqual(HttpStatusCode.NoContent, patchResponse.StatusCode);

        HttpResponseMessage openApiResponse = await client.GetAsync("/openapi/v1.json");
        Assert.AreEqual(HttpStatusCode.OK, openApiResponse.StatusCode);
        string openApi = await openApiResponse.Content.ReadAsStringAsync();
        StringAssert.Contains(openApi, "/api/usuarios");

        HttpResponseMessage scalarResponse = await client.GetAsync("/scalar/");
        Assert.AreEqual(HttpStatusCode.OK, scalarResponse.StatusCode);
        StringAssert.Contains(
            await scalarResponse.Content.ReadAsStringAsync(),
            "InstantCrud API");

        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/usuarios/{inserted.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.AreEqual(
            HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/usuarios/{inserted.Id}")).StatusCode);

        static ICrudExecutor CreateExecutor(
            IServiceProvider services,
            IEntityRegistry registry) =>
            new DapperCrudExecutor(
                services.GetRequiredService<IDbConnection>(),
                registry,
                SqliteDialect.Instance);
    }

    private static async Task CreateSchemaAsync(SqliteConnection connection) {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE USUARIO (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                NOME TEXT NOT NULL,
                ATIVO INTEGER NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
    }
}
