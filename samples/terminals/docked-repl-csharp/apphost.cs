#:sdk Aspire.AppHost.Sdk@13.6.0
#:property AspireUseCliBundle=true

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable ASPIRETERMINAL001
#pragma warning disable ASPIRECONTAINERRUNTIME001

var builder = DistributedApplication.CreateBuilder(args);
builder.AddRqlite("rqlite").WithSqlRepl();
builder.Build().Run();

sealed class RqliteResource(string name) : ContainerResource(name)
{
    public EndpointReference HttpEndpoint => new(this, "http");
}

static class RqliteBuilderExtensions
{
    public static IResourceBuilder<RqliteResource> AddRqlite(this IDistributedApplicationBuilder builder, string name)
    {
        // Demo-only database: no authentication, TLS, or persistent volume. Keep its HTTP API private;
        // dashboard authentication does not protect database operations on that endpoint.
        return builder.AddResource(new RqliteResource(name))
            .WithImage("rqlite/rqlite", "10.3.6")
            .WithHttpEndpoint(targetPort: 4001, name: "http")
            .WithHttpHealthCheck("/readyz");
    }

    public static IResourceBuilder<RqliteResource> WithSqlRepl(this IResourceBuilder<RqliteResource> builder)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return builder;
        }

        return builder.WithCommand("sql-repl", "Open SQL REPL", async context =>
        {
            var notifications = context.Services.GetRequiredService<ResourceNotificationService>();
            if (!notifications.TryGetCurrentState(context.ResourceName, out var current) ||
                current.Snapshot.State?.Text != KnownResourceStates.Running ||
                GetContainerId(current.Snapshot) is not { Length: > 0 } containerId)
            {
                return CommandResults.Failure("Wait until rqlite is running and its container ID is available.");
            }

            var runtime = await context.Services.GetRequiredService<IContainerRuntimeResolver>()
                .ResolveAsync(context.CancellationToken);
            var terminals = context.Services.GetRequiredService<TerminalService>();
            // This shell can modify or delete the entire demo database; allow only trusted dashboard users.
            var terminal = terminals.CreateTerminal(new TerminalLaunchOptions
            {
                Title = $"SQL ({builder.Resource.Name})",
                Executable = runtime.Name.ToLowerInvariant(),
                Arguments = ["exec", "-it", containerId, "/bin/rqlite", "-H", "127.0.0.1", "-p", "4001"],
                Placement = TerminalPlacement.Dock
            });
            try
            {
                terminal.Start();
                terminal.Show();
                await terminal.WaitForTextAsync("127.0.0.1:4001>", TimeSpan.FromSeconds(20), context.CancellationToken);
                // Keep the session alive for the user; TerminalService cleans up at AppHost shutdown.
                return CommandResults.Success();
            }
            catch (Exception ex) when (ex is TimeoutException or OperationCanceledException or InvalidOperationException)
            {
                await terminal.DisposeAsync();
                context.Services.GetRequiredService<ResourceLoggerService>().GetLogger(builder.Resource)
                    .LogError(ex, "Could not open the SQL REPL.");
                return CommandResults.Failure(ex is OperationCanceledException ? "Opening SQL REPL canceled." : ex.Message);
            }
        }, new CommandOptions
        {
            Description = "Open the image's SQL client in a docked terminal. Type quit before closing the tab.",
            UpdateState = context => context.ResourceSnapshot.State?.Text == KnownResourceStates.Running &&
                !string.IsNullOrEmpty(GetContainerId(context.ResourceSnapshot))
                    ? ResourceCommandState.Enabled : ResourceCommandState.Disabled
        });
    }

    private static string? GetContainerId(CustomResourceSnapshot snapshot) =>
        snapshot.Properties.FirstOrDefault(p => p.Name == "container.id")?.Value as string;
}
