using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Reflection;
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
        // Configure the builder's logger to redirect it to xunit's output & store for assertion later
        builder.Services.AddLogging(logging => logging.ClearProviders());
        builder.Services.AddSingleton(testOutputHelper);
        builder.Services.AddSingleton<ILoggerProvider, XUnitLoggerProvider>();
        builder.Services.AddSingleton<LoggerLogStore>();
        builder.Services.AddSingleton<ILoggerProvider, StoredLogsLoggerProvider>();

        // Add background service to watch resource logs, store them for assertion later, and write them to xunit's output
        builder.Services.AddSingleton<ResourceLogWatcher>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ResourceLogWatcher>());

        return builder;
    }

    /// <summary>
    /// Ensures all parameters in the application configuration have values set.
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Replaces all named volumes with anonymous volumes so they're isolated across test runs and from the volume the app uses during development.
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="resourceName"></param>
    /// <param name="useHttpClientFactory"></param>
    /// <returns></returns>
    public static HttpClient CreateHttpClient(this DistributedApplication app, string resourceName, bool useHttpClientFactory)
        => app.CreateHttpClient(resourceName, null, useHttpClientFactory);

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="resourceName"></param>
    /// <param name="endpointName"></param>
    /// <param name="useHttpClientFactory"></param>
    /// <returns></returns>
    public static HttpClient CreateHttpClient(this DistributedApplication app, string resourceName, string? endpointName, bool useHttpClientFactory)
    {
        if (useHttpClientFactory)
        {
            return app.CreateHttpClient(resourceName, endpointName);
        }

        // Don't use the HttpClientFactory to create the HttpClient so, e.g., no resilience policies are applied
        var httpClient = new HttpClient
        {
            BaseAddress = app.GetEndpoint(resourceName, endpointName)
        };

        return httpClient;
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="resourceName"></param>
    /// <param name="endpointName"></param>
    /// <param name="httpClientName"></param>
    /// <returns></returns>
    public static HttpClient CreateHttpClient(this DistributedApplication app, string resourceName, string? endpointName, string? httpClientName)
    {
        var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
        
        var httpClient = !string.IsNullOrEmpty(httpClientName) ? httpClientFactory.CreateClient(httpClientName) : httpClientFactory.CreateClient();
        httpClient.BaseAddress = app.GetEndpoint(resourceName, endpointName);

        return httpClient;
    }

    /// <inheritdoc cref = "IHost.StartAsync" />
    public static async Task StartAsync(this DistributedApplication app, bool waitForResourcesToStart, CancellationToken cancellationToken = default)
    {
        await app.StartAsync(cancellationToken);

        if (waitForResourcesToStart)
        {
            await WaitForResourcesToStartAsync(app, cancellationToken);
        }
    }

    public static IReadOnlyDictionary<string, IList<(DateTimeOffset TimeStamp, LogLevel Level, string Message, Exception? Exception)>> GetAppHostLogs(this DistributedApplication app)
    {
        var logStore = app.Services.GetRequiredService<LoggerLogStore>();
        return logStore.GetLogs();
    }

    /// <summary>
    /// Gets the logs for all resources in the application.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IReadOnlyDictionary<IResource, IReadOnlyList<LogLine>> GetResourceLogs(this DistributedApplication app)
    {
        var logStore = app.Services.GetRequiredService<ResourceLogWatcher>().LogStore;
        return logStore.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<LogLine>)entry.Value);
    }

    /// <summary>
    /// Gets the logs for the specified resource in the application.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="resourceName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IList<LogLine> GetResourceLogs(this DistributedApplication app, string resourceName)
    {
        var logStore = app.Services.GetRequiredService<ResourceLogWatcher>().LogStore;
        var resource = app.Services.GetRequiredService<DistributedApplicationModel>().Resources.FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Resource '{resourceName}' not found", nameof(resourceName));
        var logs = logStore.TryGetValue(resource, out var logLines) ? logLines : [];
        return logs;
    }

    /// <summary>
    /// Ensures no errors were logged for the application's AppHost.
    /// </summary>
    /// <param name="app"></param>
    public static void EnsureNoAppHostErrors(this DistributedApplication app)
    {
        var logs = app.GetAppHostLogs();

        var errors = logs.Where(category => category.Value.Any(log => log.Level == LogLevel.Error || log.Level == LogLevel.Critical)).ToList();
        if (errors.Count > 0)
        {
            throw new DistributedApplicationException(
                $"AppHost '{app.Services.GetRequiredService<IHostEnvironment>().ApplicationName}' logged errors: {Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, errors.Select(cat => string.Join(Environment.NewLine, cat.Value.Where(log => log.Level == LogLevel.Error || log.Level == LogLevel.Critical).Select(log => $"{log.Level} [{cat.Key}] {log.Message}"))))}");
        }
    }

    /// <summary>
    /// Ensures no errors were logged for the specified resource in the application.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="resourceName"></param>
    public static void EnsureNoResourceErrors(this DistributedApplication app, string resourceName)
    {
        app.EnsureNoResourceErrors(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Ensures no errors were logged for the specified resources in the application.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="resourcePredicate"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="DistributedApplicationException"></exception>
    public static void EnsureNoResourceErrors(this DistributedApplication app, Func<IResource, bool>? resourcePredicate = null)
    {
        var logStore = app.Services.GetRequiredService<ResourceLogWatcher>().LogStore;

        var resourcesMatched = 0;
        foreach (var (resource, logs) in logStore)
        {
            if (resourcePredicate is null || resourcePredicate(resource))
            {
                EnsureNoErrors(resource, logs);
                resourcesMatched++;
            }
        }

        if (resourcesMatched == 0 && resourcePredicate is not null)
        {
            throw new ArgumentException("No resources matched the predicate.", nameof(resourcePredicate));
        }

        static void EnsureNoErrors(IResource resource, IEnumerable<LogLine> logs)
        {
            var errors = logs.Where(l => l.IsErrorMessage).ToList();
            if (errors.Count > 0)
            {
                throw new DistributedApplicationException($"Resource '{resource.Name}' logged errors: {Environment.NewLine}{string.Join(Environment.NewLine, errors.Select(e => e.Content))}");
            }
        }
    }

    /// <summary>
    /// Attempts to apply EF migrations for the specified project by sending a request to the migrations endpoint <c>/ApplyDatabaseMigrations</c>.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="project"></param>
    /// <returns></returns>
    /// <exception cref="UnreachableException"></exception>
    public static async Task<bool> TryApplyEfMigrationsAsync(this DistributedApplication app, ProjectResource project)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(TryApplyEfMigrationsAsync));
        var projectName = project.GetName();

        // First check if the project has a migration endpoint, if it doesn't it will respond with a 404
        logger.LogInformation("Checking if project '{ProjectName}' has a migration endpoint", projectName);
        using (var checkHttpClient = app.CreateHttpClient(project.Name))
        {
            using var emptyDbContextContent = new FormUrlEncodedContent([new("context", "")]);
            using var checkResponse = await checkHttpClient.PostAsync("/ApplyDatabaseMigrations", emptyDbContextContent);
            if (checkResponse.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Project '{ProjectName}' does not have a migration endpoint", projectName);
                return false;
            }
        }

        logger.LogInformation("Attempting to apply EF migrations for project '{ProjectName}'", projectName);

        // Load the project assembly and find all DbContext types
        var projectDirectory = Path.GetDirectoryName(project.GetProjectMetadata().ProjectPath) ?? throw new UnreachableException();
#if DEBUG
        var configuration = "Debug";
#else
        var configuration = "Release";
#endif
        var projectAssemblyPath = Path.Combine(projectDirectory, "bin", configuration, "net8.0", $"{projectName}.dll");
        var projectAssembly = Assembly.LoadFrom(projectAssemblyPath);
        var dbContextTypes = projectAssembly.GetTypes().Where(t => DerivesFromDbContext(t));

        logger.LogInformation("Found {DbContextCount} DbContext types in project '{ProjectName}'", dbContextTypes.Count(), projectName);

        // Call the migration endpoint for each DbContext type
        var migrationsApplied = false;
        using var applyMigrationsHttpClient = app.CreateHttpClient(project.Name, useHttpClientFactory: false);
        applyMigrationsHttpClient.Timeout = TimeSpan.FromSeconds(240);
        foreach (var dbContextType in dbContextTypes)
        {
            logger.LogInformation("Applying migrations for DbContext '{DbContextType}' in project '{ProjectName}'", dbContextType.FullName, projectName);
            using var content = new FormUrlEncodedContent([new("context", dbContextType.AssemblyQualifiedName)]);
            using var response = await applyMigrationsHttpClient.PostAsync("/ApplyDatabaseMigrations", content);
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                migrationsApplied = true;
                logger.LogInformation("Migrations applied for DbContext '{DbContextType}' in project '{ProjectName}'", dbContextType.FullName, projectName);
            }
        }

        return migrationsApplied;
    }

    private static bool DerivesFromDbContext(Type type)
    {
        var baseType = type.BaseType;

        while (baseType is not null)
        {
            if (baseType.FullName == "Microsoft.EntityFrameworkCore.DbContext" && baseType.Assembly.GetName().Name == "Microsoft.EntityFrameworkCore")
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
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
                    HandleResourceStarted(resourceEvent.Resource, " (exited with code 0)");
                }
                else if (snapshot.State is { } state)
                {
                    if (state.Text.Contains("running", StringComparison.OrdinalIgnoreCase))
                    {
                        // Resource started
                        HandleResourceStarted(resourceEvent.Resource);
                    }
                    else if (state.Text.Contains("failedtostart", StringComparison.OrdinalIgnoreCase))
                    {
                        // Resource failed to start
                        throw new DistributedApplicationException($"Resource '{resourceName}' failed to start: {state.Text}");
                    }
                    else if (state.Text.Contains("exited", StringComparison.OrdinalIgnoreCase) && remainingResources.Contains(resourceEvent.Resource))
                    {
                        // Resource went straight to exited state
                        throw new DistributedApplicationException($"Resource '{resourceName}' exited without first running: {state.Text}");
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

        void HandleResourceStarted(IResource resource, string? suffix = null)
        {
            remainingResources.Remove(resource);
            logger.LogInformation($"Resource '{{resourceName}}' started{suffix}", resource.Name);
            if (remainingResources.Count > 0)
            {
                var resourceNames = string.Join(", ", remainingResources.Select(r => r.Name));
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

    private class StoredLogsLogger(LoggerLogStore logStore, IExternalScopeProvider scopeProvider, string categoryName) : ILogger
    {
        public string CategoryName { get; } = categoryName;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>  scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            logStore.AddLog(this, logLevel, formatter(state, exception), exception);
        }
    }

    private class StoredLogsLoggerProvider(LoggerLogStore logStore) : ILoggerProvider
    {
        private readonly LoggerExternalScopeProvider _scopeProvider = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new StoredLogsLogger(logStore, _scopeProvider, categoryName);
        }

        public void Dispose()
        {
        }
    }

    private class LoggerLogStore
    {
        private readonly ConcurrentDictionary<StoredLogsLogger, List<(DateTimeOffset TimeStamp, LogLevel Level, string Message, Exception? Exception)>> _store = [];

        public void AddLog(StoredLogsLogger logger, LogLevel level, string message, Exception? exception)
        {
            _store.GetOrAdd(logger, _ => []).Add((DateTimeOffset.Now, level, message, exception));
        }

        public IReadOnlyDictionary<string, IList<(DateTimeOffset TimeStamp, LogLevel Level, string Message, Exception? Exception)>> GetLogs()
        {
            return _store.ToDictionary(entry => entry.Key.CategoryName, entry => (IList<(DateTimeOffset, LogLevel, string, Exception?)>)entry.Value);
        }
    }
}
