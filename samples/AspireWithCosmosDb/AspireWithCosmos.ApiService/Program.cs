using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddHostedService<DatabaseBootstrapper>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.AddAzureCosmosClient("cosmos");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseExceptionHandler();

// create new todos
app.MapPost("/todos", async (Todo todo, CosmosClient cosmosClient) =>
    (await cosmosClient.GetAppDataContainer().CreateItemAsync<Todo>(todo)).Resource
);

// get all the todos
app.MapGet("/todos", (CosmosClient cosmosClient) =>
    cosmosClient.GetAppDataContainer().GetItemLinqQueryable<Todo>(allowSynchronousQueryExecution: true).ToList()
);

app.MapPut("/todos/{id}", async (string id, Todo todo, CosmosClient cosmosClient) =>
    (await cosmosClient.GetAppDataContainer().ReplaceItemAsync<Todo>(todo, id)).Resource
);

app.MapDelete("/todos/{userId}/{id}", async (string userId, string id, CosmosClient cosmosClient) =>
{
    await cosmosClient.GetAppDataContainer().DeleteItemAsync<Todo>(id, new PartitionKey(userId));
    return Results.Ok();
});

app.MapDefaultEndpoints();

app.Run();

// The Todo service model used for transmitting data
public record Todo(string Description, string id, string UserId, bool IsComplete = false)
{
    // partiion the todos by user id
    internal static string UserIdPartitionKey = "/UserId";
}

// Background service used to scaffold the Cosmos DB/Container
public class DatabaseBootstrapper(CosmosClient cosmosClient) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await cosmosClient.CreateDatabaseIfNotExistsAsync("tododb");
        var database = cosmosClient.GetDatabase("tododb");
        await database.CreateContainerIfNotExistsAsync(new ContainerProperties("todos", Todo.UserIdPartitionKey));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// Convenience class for reusing boilerplate code
public static class CosmosClientTodoAppExtensions
{
    public static Container GetAppDataContainer(this CosmosClient cosmosClient)
    {
        var database = cosmosClient.GetDatabase("tododb");
        var todos = database.GetContainer("todos");

        if(todos == null) throw new ApplicationException("Cosmos DB collection missing.");

        return todos;
    }
}