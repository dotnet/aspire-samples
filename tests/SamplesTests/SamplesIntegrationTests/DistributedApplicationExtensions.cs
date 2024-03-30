using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SamplesIntegrationTests;

public static class DistributedApplicationExtensions
{
    public static TBuilder WriteOutputTo<TBuilder>(this TBuilder builder, ITestOutputHelper testOutputHelper)
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        var xunitLoggerProvider = new XUnitLoggerProvider(testOutputHelper);
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(xunitLoggerProvider);
        });

        builder.Services.AddSingleton(sp => new ResourceLogWatcher(
            sp.GetRequiredService<ResourceNotificationService>(),
            sp.GetRequiredService<ResourceLoggerService>(),
            testOutputHelper));

        builder.Services.AddHostedService(sp => sp.GetRequiredService<ResourceLogWatcher>());

        return builder;
    }

    public static TBuilder WithRandomParameterValues<TBuilder>(this TBuilder builder)
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        var parameters = builder.Resources.OfType<ParameterResource>().Where(p => !p.IsConnectionString).ToList();
        foreach (var parameter in parameters)
        {
            builder.Configuration[$"Parameters:{parameter.Name}"] = parameter.Secret
                ? PasswordGenerator.Generate(16, true, true, true, false, 1, 1, 1, 0)
                : Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        }

        return builder;
    }

    public static TBuilder WithAnonymousVolumeNames<TBuilder>(this TBuilder builder)
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        foreach (var resource in builder.Resources)
        {
            if (resource.TryGetAnnotationsOfType<ContainerMountAnnotation>(out var mounts))
            {
                var mountsList = mounts.ToList();

                for (var i = 0; i < mountsList.Count; i++)
                {
                    var mount = mountsList[i];
                    if (mount.Type == ContainerMountType.Volume)
                    {
                        var newMount = new ContainerMountAnnotation(null, mount.Target, mount.Type, mount.IsReadOnly);
                        resource.Annotations.Remove(mount);
                        resource.Annotations.Add(newMount);
                    }
                }
            }
        }

        return builder;
    }

    public static HttpClient CreateHttpClient(this DistributedApplication app, string resourceName, bool disableResilience)
        => app.CreateHttpClient(resourceName, null, disableResilience);

    public static HttpClient CreateHttpClient(this DistributedApplication app, string resourceName, string? endpointName, bool disableResilience)
    {
        if (!disableResilience)
        {
            return app.CreateHttpClient(resourceName, endpointName);
        }

        // Don't use the HttpClientFactory to create the HttpClient so no resilience policies are applied
        var httpClient = new HttpClient
        {
            BaseAddress = app.GetEndpoint(resourceName, endpointName)
        };

        return httpClient;
    }

    public static async Task StartAsync(this DistributedApplication app, bool waitForResourcesToStart, CancellationToken cancellationToken = default)
    {
        await app.StartAsync(cancellationToken);

        if (waitForResourcesToStart)
        {
            await WaitForResourcesToStartAsync(app, cancellationToken);
        }
    }

    public static IReadOnlyDictionary<IResource, IReadOnlyList<LogLine>> GetResourceLogs(this DistributedApplication app)
    {
        var logStore = app.Services.GetRequiredService<ResourceLogWatcher>().LogStore;
        return logStore.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<LogLine>)entry.Value);
    }

    public static IList<LogLine> GetResourceLogs(this DistributedApplication app, string resourceName)
    {
        var logStore = app.Services.GetRequiredService<ResourceLogWatcher>().LogStore;
        var resource = app.Services.GetRequiredService<DistributedApplicationModel>().Resources.FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Resource '{resourceName}' not found", nameof(resourceName));
        var logs = logStore.TryGetValue(resource, out var logLines) ? logLines : [];
        return logs;
    }

    public static void EnsureNoResourceErrors(this DistributedApplication app, string? resourceName = null)
    {
        var logStore = app.Services.GetRequiredService<ResourceLogWatcher>().LogStore;

        if (!string.IsNullOrEmpty(resourceName))
        {
            var resource = app.Services.GetRequiredService<DistributedApplicationModel>().Resources.FirstOrDefault(r => r.Name == resourceName)
                ?? throw new ArgumentException($"Resource with name '{resourceName}' could not be found.", nameof(resourceName));
            var logs = logStore[resource];
            EnsureNoErrors(resource, logs);
        }
        else
        {
            foreach (var (resource, logs) in logStore)
            {
                EnsureNoErrors(resource, logs);
            }
        }

        void EnsureNoErrors(IResource resource, IEnumerable<LogLine> logs)
        {
            var errors = logs.Where(l => l.IsErrorMessage).ToList();
            if (errors.Count > 0)
            {
                throw new DistributedApplicationException($"Resource '{resource.Name}' logged errors: {Environment.NewLine}{string.Join(Environment.NewLine, errors.Select(e => e.Content))}");
            }
        }
    }

    private static readonly TimeSpan resourceStartDefaultTimeout = TimeSpan.FromSeconds(30);

    private static async Task WaitForResourcesToStartAsync(DistributedApplication app, CancellationToken cancellationToken = default)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DistributedApplicationExtensions));
        var resources = GetWaitableResources(app).ToList();
        var remainingResources = new HashSet<IResource>(resources);

        logger.LogInformation("Waiting on {resourcesToStartCount} resources to start", remainingResources.Count);

        var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var timeoutCts = new CancellationTokenSource(resourceStartDefaultTimeout);
        var cts = cancellationToken == default
            ? timeoutCts
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        await foreach (var resourceEvent in notificationService.WatchAsync().WithCancellation(cts.Token))
        {
            var resourceName = resourceEvent.Resource.Name;
            if (remainingResources.Contains(resourceEvent.Resource))
            {
                // TODO: Handle replicas
                var snapshot = resourceEvent.Snapshot;
                if (snapshot.ExitCode is { } exitCode)
                {
                    if (exitCode != 0)
                    {
                        // Error starting resource
                        throw new DistributedApplicationException($"Resource '{resourceName}' exited with exit code {exitCode}");
                    }

                    // Resource exited cleanly
                    HandleResourceStarted(resourceEvent.Resource);
                }
                else if (snapshot.State is { } state)
                {
                    if (state.Text.Contains("running", StringComparison.OrdinalIgnoreCase)
                        || state.Text.Contains("exited", StringComparison.OrdinalIgnoreCase))
                    {
                        // Resource started or exited
                        HandleResourceStarted(resourceEvent.Resource);
                    }
                    else if (state.Text.Contains("failedtostart", StringComparison.OrdinalIgnoreCase))
                    {
                        // Resource failed to start
                        throw new DistributedApplicationException($"Resource '{resourceName}' failed to start: {state.Text}");
                    }
                    else if (state.Text.Contains("starting", StringComparison.OrdinalIgnoreCase)
                             || state.Text.Contains("hidden", StringComparison.OrdinalIgnoreCase))
                    {
                        // Resource is still starting
                        continue;
                    }
                    else if (!string.IsNullOrEmpty(state.Text))
                    {
                        logger.LogWarning("Unknown resource state encountered: {state}", state.Text);
                    }
                }
            }

            if (remainingResources.Count == 0)
            {
                logger.LogInformation("All resources started successfully");
                break;
            }
        }

        void HandleResourceStarted(IResource resource)
        {
            remainingResources.Remove(resource);
            logger.LogInformation("Resource '{resourceName}' started", resource.Name);
            if (remainingResources.Count > 0)
            {
                var resourceNames = string.Join(", ", remainingResources);
                logger.LogInformation("Still waiting on {resourcesToStartCount} resources to start: {resourcesToStart}", remainingResources.Count, resourceNames);
            }
        }
    }

    private static IEnumerable<IResource> GetWaitableResources(this DistributedApplication app)
    {
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        return appModel.Resources.Where(r => r is ContainerResource || r is ExecutableResource || r is ProjectResource || r is AzureConstructResource);
    }

    private sealed class ResourceLogWatcher(
        ResourceNotificationService resourceNotification,
        ResourceLoggerService resourceLoggerService,
        ITestOutputHelper testOutputHelper)
        : BackgroundService
    {
        public ConcurrentDictionary<IResource, List<LogLine>> LogStore { get; } = [];

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return WatchNotifications(stoppingToken);
        }

        private async Task WatchNotifications(CancellationToken stoppingToken)
        {
            var loggingResourceIds = new HashSet<string>();
            var logWatchTasks = new List<Task>();

            await foreach (var resourceEvent in resourceNotification.WatchAsync().WithCancellation(stoppingToken))
            {
                if (loggingResourceIds.Add(resourceEvent.ResourceId))
                {
                    // Start watching the logs for this resource ID
                    logWatchTasks.Add(WatchLogs(resourceEvent.Resource, resourceEvent.ResourceId, stoppingToken));
                }
                if (resourceEvent.Snapshot.ExitCode is null && resourceEvent.Snapshot.State is { } state && !string.IsNullOrEmpty(state.Text))
                {
                    // Log resource state change
                    testOutputHelper.WriteLine("Resource '{0}' of type '{1}' -> {2}", resourceEvent.ResourceId, resourceEvent.Resource.GetType().Name, state.Text);
                }
                else if (resourceEvent.Snapshot.ExitCode is { } exitCode)
                {
                    // Log resource exit code
                    testOutputHelper.WriteLine("Resource '{0}' exited with code {1}", resourceEvent.ResourceId, exitCode);
                }
            }

            await Task.WhenAll(logWatchTasks);
        }

        private async Task WatchLogs(IResource resource, string resourceId, CancellationToken stoppingToken)
        {
            await foreach (var logEvent in resourceLoggerService.WatchAsync(resourceId).WithCancellation(stoppingToken))
            {
                foreach (var line in logEvent)
                {
                    LogStore.GetOrAdd(resource, _ => []).Add(line);
                    var kind = line.IsErrorMessage ? "error" : "log";
                    testOutputHelper.WriteLine("Resource '{0}' {1}: {2}", resource.Name, kind, line.Content);
                }
            }
        }
    }

    private class XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName) : ILogger
    {
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => scopeProvider.Push(state);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var sb = new StringBuilder();

            sb.Append(GetLogLevelString(logLevel))
              .Append(" [").Append(categoryName).Append("] ")
              .Append(formatter(state, exception));

            if (exception is not null)
            {
                sb.Append('\n').Append(exception);
            }

            // Append scopes
            scopeProvider.ForEachScope((scope, state) =>
            {
                state.Append("\n => ");
                state.Append(scope);
            }, sb);

            testOutputHelper.WriteLine(sb.ToString());
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }
    }

    private class XUnitLogger<T>(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider)
        : XUnitLogger(testOutputHelper, scopeProvider, typeof(T).FullName ?? ""), ILogger<T>
    {
    }

    private class XUnitLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
    {
        private readonly LoggerExternalScopeProvider _scopeProvider = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(testOutputHelper, _scopeProvider, categoryName);
        }

        public void Dispose()
        {
        }
    }
}
