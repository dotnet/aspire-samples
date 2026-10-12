using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;
using Notes.Api;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddRedisClient("redis");
builder.Services.AddSingleton<INotesStore, RedisNotesStore>();
builder.Services.AddProblemDetails();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 10 * 1024);

var app = builder.Build();
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()!.Error;
    var (status, title) = error switch
    {
        RedisException => (503, "Redis is unavailable"),
        BadHttpRequestException badRequest => (badRequest.StatusCode, "Invalid request"),
        _ => (500, "An unexpected error occurred")
    };
    await Results.Problem(statusCode: status, title: title).ExecuteAsync(context);
}));
app.MapDefaultEndpoints();

// Demo-only, unauthenticated CRUD routes: every caller can read, replace, and delete notes.
// Use non-sensitive, disposable data and keep this HTTP API private.
var notes = app.MapGroup("/notes");
notes.AddEndpointFilter(async (context, next) =>
{
    if (context.HttpContext.Request.RouteValues["id"] is string id &&
        !Regex.IsMatch(id, @"\A[a-z0-9-]{1,40}\z", RegexOptions.CultureInvariant))
    {
        return Results.Problem(statusCode: 400, title: "Use 1-40 lowercase letters, digits or hyphens for the note ID");
    }
    return await next(context);
});

notes.MapGet("/", async (INotesStore store) => Results.Ok(await store.ListAsync()));
notes.MapGet("/{id}", async (string id, INotesStore store) =>
    await store.GetAsync(id) is { } note
        ? Results.Ok(note)
        : Results.Problem(statusCode: 404, title: "Note not found"));
notes.MapPut("/{id}", async (string id, JsonElement body, INotesStore store) =>
{
    if (body.ValueKind != JsonValueKind.Object ||
        !body.TryGetProperty("text", out var value) || value.ValueKind != JsonValueKind.String ||
        value.GetString() is not { } text || string.IsNullOrWhiteSpace(text) || text.Length > 1000)
    {
        return Results.Problem(statusCode: 400, title: "Supply text containing 1-1000 characters, not just whitespace");
    }

    var note = new Note(id, text);
    return await store.SetAsync(note)
        ? Results.Created($"/notes/{id}", note)
        : Results.Ok(note);
});
notes.MapDelete("/{id}", async (string id, INotesStore store) =>
    await store.DeleteAsync(id)
        ? Results.NoContent()
        : Results.Problem(statusCode: 404, title: "Note not found"));

app.Run();

public partial class Program;
