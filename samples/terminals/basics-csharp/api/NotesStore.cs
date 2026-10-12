using StackExchange.Redis;

namespace Notes.Api;

public record Note(string Id, string Text);

public interface INotesStore
{
    Task<Note[]> ListAsync();
    Task<Note?> GetAsync(string id);
    Task<bool> SetAsync(Note note);
    Task<bool> DeleteAsync(string id);
}

public sealed class RedisNotesStore(IConnectionMultiplexer connection) : INotesStore
{
    private readonly IDatabase _database = connection.GetDatabase();
    private const string Key = "notes";

    public async Task<Note[]> ListAsync() =>
        (await _database.HashGetAllAsync(Key))
            .Select(entry => new Note(entry.Name.ToString(), entry.Value.ToString()))
            .OrderBy(note => note.Id, StringComparer.Ordinal)
            .ToArray();

    public async Task<Note?> GetAsync(string id)
    {
        var text = await _database.HashGetAsync(Key, id);
        return text.IsNull ? null : new Note(id, text.ToString());
    }

    public Task<bool> SetAsync(Note note) => _database.HashSetAsync(Key, note.Id, note.Text);
    public Task<bool> DeleteAsync(string id) => _database.HashDeleteAsync(Key, id);
}
