using System.Data;
using System.Reflection;
using InstaCrud.AspNetCore;
using InstaCrud.Dapper;
using InstaCrud.Interfaces;
using InstaCrud.Sql;
using Microsoft.Data.Sqlite;

const string connectionString = "Data Source=instantcrud-sample.db";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));
builder.Services.AddInstantCrud(options => {
    options.ApiTitle = "InstantCrud SQLite Sample";
    options
        .AddEntitiesFromAssembly(Assembly.GetExecutingAssembly())
        .UseExecutor((services, registry) =>
            new DapperCrudExecutor(
                services.GetRequiredService<IDbConnection>(),
                registry,
                SqliteDialect.Instance));
});

WebApplication app = builder.Build();

await InitializeDatabaseAsync(connectionString);

app.MapGet("/", () => Results.Redirect("/scalar/"))
    .ExcludeFromDescription();
app.MapInstantCrud();

await app.RunAsync();

static async Task InitializeDatabaseAsync(string connectionString) {
    await using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    await using SqliteCommand command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE IF NOT EXISTS USUARIO (
            ID INTEGER PRIMARY KEY AUTOINCREMENT,
            NOME TEXT NOT NULL,
            EMAIL TEXT NOT NULL,
            ATIVO INTEGER NOT NULL,
            CRIADO_EM TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS ENDERECO (
            ID INTEGER PRIMARY KEY AUTOINCREMENT,
            RUA TEXT NOT NULL,
            NUMERO INTEGER NOT NULL,
            BAIRRO TEXT NOT NULL,
            COMPLEMENTO TEXT NULL,
            ATIVO INTEGER NOT NULL,
            CRIADO_EM TEXT NOT NULL
        );
        """;
    await command.ExecuteNonQueryAsync();
}
