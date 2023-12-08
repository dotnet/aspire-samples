using Dapper;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace DatabaseContainers.ApiService;

public static class ApiEndpoints
{
    public static WebApplication MapTodosApi(this WebApplication app)
    {
        app.MapGet("/todos", async (NpgsqlDataSource db) =>
        {
            const string sql = """
                SELECT Id, Title, IsComplete
                FROM Todos
                """;
            var connection = db.CreateConnection();
            return await connection.QueryAsync<Todo>(sql);
        });

        app.MapGet("/todos/{id}", async (int id, NpgsqlDataSource db) =>
        {
            const string sql = """
                SELECT Id, Title, IsComplete
                FROM Todos
                WHERE Id = @id
                """;
            var connection = db.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Todo>(sql, new { id }) is { } todo
                ? Results.Ok(todo)
                : Results.NotFound();
        });

        return app;
    }

    public static WebApplication MapCatalogApi(this WebApplication app)
    {
        app.MapGet("/catalog", async (MySqlDataSource db) =>
        {
            const string sql = """
                SELECT Id, Name, Description, Price
                FROM catalog
                """;
            var connection = db.CreateConnection();
            return await connection.QueryAsync<CatalogItem>(sql);
        });

        app.MapGet("/catalog/{id}", async (int id, MySqlDataSource db) =>
        {
            const string sql = """
                SELECT Id, Name, Description, Price
                FROM catalog
                WHERE Id = @id
                """;
            var connection = db.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<CatalogItem>(sql, new { id }) is { } item
                ? Results.Ok(item)
                : Results.NotFound();
        });

        return app;
    }

    public static WebApplication MapAddressBookApi(this WebApplication app)
    {
        app.MapGet("/addressbook", async (SqlConnection db) =>
        {
            const string sql = """
                SELECT Id, FirstName, LastName, Email, Phone
                FROM Contacts
                """;
            return await db.QueryAsync<Contact>(sql);
        });

        app.MapGet("/addressbook/{id}", async (int id, SqlConnection db) =>
        {
            const string sql = """
                SELECT Id, FirstName, LastName, Email, Phone
                FROM Contacts
                WHERE Id = @id
                """;
            return await db.QueryFirstOrDefaultAsync<Contact>(sql, new { id }) is { } contact
                ? Results.Ok(contact)
                : Results.NotFound();
        });

        return app;
    }
}

public record Todo(int Id, string Title, bool IsComplete);

public record CatalogItem(int Id, string Name, string Description, decimal Price);

public record Contact(int Id, string FirstName, string LastName, string Email, string? Phone);
