#:package Aspire.Hosting.Redis@13.6.0
#:sdk Aspire.AppHost.Sdk@13.6.0
#:property AspireUseCliBundle=true

using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable ASPIRETERMINAL001

var builder = DistributedApplication.CreateBuilder(args);
// Trusted local demo: Redis data is disposable and its authenticated REPL requires a private dashboard.
var redis = builder.AddRedis("redis").WithRepl();
// The reused HTTP API has no authentication; keep its endpoint private and use non-sensitive notes.
var api = builder.AddProject("api", "../basics-csharp/api/Notes.Api.csproj")
    .WithReference(redis)
    .WithHttpHealthCheck("/health")
    .WaitFor(redis);
var slumber = builder.AddDockerfile("slumber", "../basics-csharp/slumber")
    .WithBindMount("./requests.yml", "/home/slumber/slumber.yml", isReadOnly: true)
    .WithEnvironment("BASE_URL", api.GetEndpoint("http"))
    .WaitFor(api)
    .WithTerminal();

using var gate = new SemaphoreSlim(1);
if (builder.ExecutionContext.IsRunMode)
{
    slumber.WithCommand("walkthrough", "Run terminal walkthrough", async context =>
    {
        if (!await gate.WaitAsync(0, context.CancellationToken))
        {
            return CommandResults.Failure("A walkthrough is already running.");
        }

        var logger = context.Services.GetRequiredService<ResourceLoggerService>().GetLogger(slumber.Resource);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        var ct = timeout.Token;
        try
        {
            var interactions = context.Services.GetRequiredService<IInteractionService>();
            if (interactions.IsAvailable)
            {
                var confirmation = await interactions.PromptConfirmationAsync(
                    "Restart Slumber and run automation?",
                    "Slumber will restart to put its terminal in a known state, clearing its current selection and request history. " +
                    "The walkthrough will also reset the reserved 'automation' note, then create, read, update, and delete it. " +
                    "Other notes are left unchanged. Do not type into or resize Slumber until the walkthrough finishes.",
                    new MessageBoxInteractionOptions
                    {
                        PrimaryButtonText = "Restart and run",
                        SecondaryButtonText = "Cancel"
                    },
                    ct);
                if (confirmation.Canceled || !confirmation.Data)
                {
                    return CommandResults.Canceled();
                }
            }
            else
            {
                logger.LogInformation("Non-interactive walkthrough: restarting Slumber and resetting the reserved automation note without a confirmation dialog.");
            }

            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            var notifications = context.Services.GetRequiredService<ResourceNotificationService>();
            await notifications.WaitForResourceHealthyAsync(api.Resource.Name, ct);
            using var client = new HttpClient { BaseAddress = new Uri(api.GetEndpoint("http").Url) };
            // Reset only this walkthrough's note. All demonstrated CRUD operations go through the TUI.
            using var reset = await client.DeleteAsync("/notes/automation", ct);
            if (reset.StatusCode != HttpStatusCode.NotFound)
            {
                reset.EnsureSuccessStatusCode();
            }

            // A new container and non-persisted recipes prevent old responses from satisfying waits.
            if (!notifications.TryGetCurrentState(context.ResourceName, out var previous))
            {
                return CommandResults.Failure("Slumber has no running snapshot.");
            }
            var previousId = ContainerId(previous.Snapshot);
            var commands = context.Services.GetRequiredService<ResourceCommandService>();
            var restart = await commands.ExecuteCommandAsync(context.ResourceName, "restart", ct);
            if (!restart.Success)
            {
                return restart;
            }
            await notifications.WaitForResourceAsync(slumber.Resource.Name,
                e => e.Snapshot.State?.Text == KnownResourceStates.Running &&
                    ContainerId(e.Snapshot) is { Length: > 0 } id && id != previousId, ct);

            var terminals = context.Services.GetRequiredService<TerminalService>();
            if (!terminals.TryGetTerminal($"resource:{slumber.Resource.Name}:0", out var terminal))
            {
                return CommandResults.Failure("Slumber's terminal is not registered.");
            }
            await using (terminal)
            {
                await terminal.WaitForTextAsync("No request history", cancellationToken: ct);
                await terminal.SendKeyAsync(AspireTerminalKey.R, ct);
                await SendAndCheckAsync(terminal, client, "201 Created", "Created by terminal automation", ct);
                logger.LogInformation("Create: terminal returned 201 and API contains the new note.");

                await NextAsync(terminal, "read_note", ct);
                await SendAndCheckAsync(terminal, client, "200 OK", "Created by terminal automation", ct);
                logger.LogInformation("Read: terminal and API agree on the note text.");

                await NextAsync(terminal, "update_note", ct);
                await SendAndCheckAsync(terminal, client, "200 OK", "Updated by terminal automation", ct);
                logger.LogInformation("Update: terminal and API agree on the changed text.");

                await NextAsync(terminal, "delete_note", ct);
                await terminal.SendKeyAsync(AspireTerminalKey.Enter, ct);
                await terminal.WaitForTextAsync("204 No Content", cancellationToken: ct);
                using var deleted = await client.GetAsync("/notes/automation", ct);
                if (deleted.StatusCode != HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException($"Expected 404 after delete, got {(int)deleted.StatusCode}.");
                }
                logger.LogInformation("Walkthrough passed: create, read, update and delete through Slumber.");
            }

            timeout.CancelAfter(Timeout.InfiniteTimeSpan);
            if (interactions.IsAvailable)
            {
                await interactions.PromptMessageBoxAsync(
                    "Terminal walkthrough complete",
                    "Create, read, update, and delete all passed in Slumber, and the API checks confirmed each result. " +
                    "The automation note has been deleted. Slumber is ready for manual use or another walkthrough.",
                    new MessageBoxInteractionOptions { PrimaryButtonText = "Done" },
                    context.CancellationToken);
            }
            return CommandResults.Success();
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Terminal walkthrough canceled.");
            return CommandResults.Canceled();
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException or HttpRequestException or InvalidOperationException)
        {
            logger.LogError(ex, "Terminal walkthrough failed. Inspect Slumber's screen; rerun to start a fresh session.");
            return CommandResults.Failure(ex is OperationCanceledException ? "Walkthrough canceled or timed out." : ex.Message);
        }
        finally
        {
            gate.Release();
        }
    }, new CommandOptions
    {
        Description = "Restarts Slumber and drives CRUD through its terminal. Do not type into Slumber during the run.",
        IconName = "Play",
        IsHighlighted = true,
        UpdateState = context => context.ResourceSnapshot.State?.Text == KnownResourceStates.Running
            ? ResourceCommandState.Enabled : ResourceCommandState.Disabled
    });
}

builder.Build().Run();

static string? ContainerId(CustomResourceSnapshot snapshot) =>
    snapshot.Properties.FirstOrDefault(p => p.Name == "container.id")?.Value as string;

static async Task NextAsync(AspireTerminal terminal, string recipe, CancellationToken ct)
{
    await terminal.SendKeyAsync(AspireTerminalKey.R, ct);
    await terminal.SendKeyAsync(AspireTerminalKey.Down, ct);
    await terminal.WaitForTextAsync(recipe, cancellationToken: ct);
    await terminal.WaitForTextAsync("No request history", cancellationToken: ct);
}

static async Task SendAndCheckAsync(AspireTerminal terminal, HttpClient client, string status, string text, CancellationToken ct)
{
    await terminal.SendKeyAsync(AspireTerminalKey.Enter, ct);
    await terminal.WaitForTextAsync(status, cancellationToken: ct);
    await terminal.WaitForTextAsync(text, cancellationToken: ct);
    var note = await client.GetFromJsonAsync<Note>("/notes/automation", ct);
    if (note is not { Id: "automation" } || note.Text != text)
    {
        throw new InvalidOperationException("The API did not return the text shown in Slumber.");
    }
}

record Note(string Id, string Text);
