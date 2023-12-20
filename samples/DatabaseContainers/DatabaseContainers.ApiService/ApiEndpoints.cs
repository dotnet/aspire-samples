using Dapper;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace DatabaseContainers.ApiService;

public static class ApiEndpoints
{
    public static WebApplication MapTodosApi(this WebApplication app)
    {
        app.MapGet("/todos", async (NpgsqlConnection db) =>
        {
            const string sql = """
                SELECT Id, Title, IsComplete
                FROM Todos
                """;

            return await db.QueryAsync<Todo>(sql);
        });

        app.MapGet("/todos/{id}", async (int id, NpgsqlConnection db) =>
        {
            const string sql = """
                SELECT Id, Title, IsComplete
                FROM Todos
                WHERE Id = @id
                """;

            return await db.QueryFirstOrDefaultAsync<Todo>(sql, new { id }) is { } todo
                ? Results.Ok(todo)
                : Results.NotFound();
        });

        return app;
    }

    public static WebApplication MapCatalogApi(this WebApplication app)
    {
        app.MapGet("/catalog", async (MySqlConnection db) =>
        {
            const string sql = """
                SELECT Id, Name, Description, Price
                FROM catalog
                """;

            return await db.QueryAsync<CatalogItem>(sql);
        });

        app.MapGet("/catalog/{id}", async (int id, MySqlConnection db) =>
        {
            const string sql = """
                SELECT Id, Name, Description, Price
                FROM catalog
                WHERE Id = @id
                """;

            return await db.QueryFirstOrDefaultAsync<CatalogItem>(sql, new { id }) is { } item
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
