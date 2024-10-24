using Refit;

namespace AspireWithCosmos.Web;

public interface ITodoApiClient
{
    [Post("/todos")]
    Task<Todo> Create(Todo todo);

    [Get("/todos")]
    Task<List<Todo>> Retrieve();

    [Put("/todos/{id}")]
    Task<Todo> Update(string id, Todo todo);

    [Delete("/todos/{userId}/{id}")]
    Task Delete(string userId, string id);
}

public class TodoApiClient(HttpClient httpClient) : ITodoApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<Todo> Create(Todo todo)
        => await RestService.For<ITodoApiClient>(_httpClient).Create(todo);

    public async Task<List<Todo>> Retrieve()
        => await RestService.For<ITodoApiClient>(_httpClient).Retrieve();

    public async Task<Todo> Update(string id, Todo todo)
        => await RestService.For<ITodoApiClient>(_httpClient).Update(id, todo);

    public async Task Delete(string userId, string id)
        => await RestService.For<ITodoApiClient>(_httpClient).Delete(userId, id);
}

// The Todo service model used for transmitting data
public record Todo(string Description, string UserId)
{
    public required string id { get; set; }
    public bool IsComplete { get; set; }
}
