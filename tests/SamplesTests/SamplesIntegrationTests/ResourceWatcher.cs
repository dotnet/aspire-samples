using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SamplesIntegrationTests;

internal sealed class ResourceWatcher(
    DistributedApplicationModel appModel,
    ResourceNotificationService resourceNotification,
    ResourceLoggerService resourceLoggerService,
    IServiceProvider serviceProvider,
    ILogger<ResourceWatcher> logger)
    : BackgroundService
{
    private readonly TimeSpan resourceStartDefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly HashSet<string> _waitingToStartResources = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _startedResources = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _waitingToStopResources = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _stoppedResources = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (ResourceStateSnapshot? Snapshot, int? ExitCode)> _resourceState = [];
    private readonly TaskCompletionSource _resourcesStartedTcs = new();
    private readonly TaskCompletionSource _resourcesStoppedTcs = new();
    //private readonly CancellationTokenSource _shutdownCts = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Resource watcher started");

        var statusWatchableResources = GetStatusWatchableResources().ToList();
        statusWatchableResources.ForEach(r =>
        {
            _waitingToStartResources.Add(r.Name);
            _waitingToStopResources.Add(r.Name);
        });
        logger.LogInformation("Watching {resourceCount} resources for start/stop changes", statusWatchableResources.Count);

        //stoppingToken.Register(() =>
        //{
        //    logger.LogInformation("Resource watcher stopping");

        //    // Allow 30 seconds after stopping signal for resources to stop
        //    _shutdownCts.CancelAfter(TimeSpan.FromSeconds(30));
        //});

        await WatchNotifications();

        logger.LogInformation("Resource watcher stopped");
    }

    public override void Dispose()
    {
        _resourcesStartedTcs.TrySetException(new DistributedApplicationException("Resource watcher was disposed while waiting for resources to start, likely due to a timeout"));
        _resourcesStoppedTcs.TrySetException(new DistributedApplicationException("Resource watcher was disposed while waiting for resources to stop, likely due to a timeout"));
    }

    public Task WaitForResourcesToStart() => _resourcesStartedTcs.Task;

    public Task WaitForResourcesToStop() => _resourcesStoppedTcs.Task;

    private async Task WatchNotifications()
    {
        var logStore = serviceProvider.GetService<ResourceLogStore>();
        var testOutputHelper = serviceProvider.GetService<ITestOutputHelper>();
        var watchingLogs = logStore is not null || testOutputHelper is not null;
        var loggingResourceIds = new HashSet<string>();
        var logWatchTasks = new List<Task>();

        logger.LogInformation("Waiting on {resourcesToStartCount} resources to start", _waitingToStartResources.Count);

        await foreach (var resourceEvent in resourceNotification.WatchAsync())
        {
            var resourceName = resourceEvent.Resource.Name;
            var resourceId = resourceEvent.ResourceId;

            if (watchingLogs && loggingResourceIds.Add(resourceId))
            {
                // Start watching the logs for this resource ID
                logWatchTasks.Add(WatchResourceLogs(logStore, testOutputHelper, resourceEvent.Resource, resourceId));
            }

            _resourceState.TryGetValue(resourceName, out var prevState);
            _resourceState[resourceName] = (resourceEvent.Snapshot.State, resourceEvent.Snapshot.ExitCode);

            if (resourceEvent.Snapshot.ExitCode is null && resourceEvent.Snapshot.State is { } newState && !string.IsNullOrEmpty(newState.Text))
            {
                if (!string.Equals(prevState.Snapshot?.Text, newState.Text, StringComparison.OrdinalIgnoreCase))
                {
                    // Log resource state change
                    logger.LogInformation("Resource '{resourceName}' of type '{resourceType}' changed state: {oldState} -> {newState}", resourceId, resourceEvent.Resource.GetType().Name, prevState.Snapshot?.Text ?? "[null]", newState.Text);

                    if (newState.Text.Contains("running", StringComparison.OrdinalIgnoreCase))
                    {
                        // Resource started
                        HandleResourceStarted(resourceEvent.Resource);
                    }
                    else if (newState.Text.Contains("failedtostart", StringComparison.OrdinalIgnoreCase))
                    {
                        // Resource failed to start
                        HandleResourceStartError(resourceName, $"Resource '{resourceName}' failed to start: {newState.Text}");
                    }
                    else if (newState.Text.Contains("exited", StringComparison.OrdinalIgnoreCase))
                    {
                        if (_waitingToStartResources.Contains(resourceEvent.Resource.Name))
                        {
                            // Resource went straight to exited state
                            HandleResourceStartError(resourceName, $"Resource '{resourceName}' exited without first running: {newState.Text}");
                        }

                        // Resource stopped
                        HandleResourceStopped(resourceEvent.Resource);
                    }
                    else if (newState.Text.Contains("starting", StringComparison.OrdinalIgnoreCase)
                             || newState.Text.Contains("hidden", StringComparison.OrdinalIgnoreCase))
                    {
                        // Resource is still starting
                    }
                    else if (!string.IsNullOrEmpty(newState.Text))
                    {
                        logger.LogWarning("Unknown resource state encountered: {state}", newState.Text);
                    }
                }
            }
            else if (resourceEvent.Snapshot.ExitCode is { } exitCode)
            {
                if (exitCode != 0)
                {
                    if (_waitingToStartResources.Remove(resourceName))
                    {
                        // Error starting resource
                        HandleResourceStartError(resourceName, $"Resource '{resourceName}' exited with exit code {exitCode}");
                    }
                    HandleResourceStopError(resourceName, $"Resource '{resourceName}' exited with exit code {exitCode}");
                }
                else
                {
                    // Resource exited cleanly
                    HandleResourceStarted(resourceEvent.Resource, " (exited with code 0)");
                    HandleResourceStopped(resourceEvent.Resource, " (exited with code 0)");
                }
            }

            if (_waitingToStartResources.Count == 0)
            {
                logger.LogInformation("All resources started");
                _resourcesStartedTcs.TrySetResult();
            }

            if (_waitingToStopResources.Count == 0)
            {
                logger.LogInformation("All resources stopped");
                _resourcesStoppedTcs.TrySetResult();
            }
        }

        void HandleResourceStartError(string resourceName, string message)
        {
            if (_waitingToStartResources.Remove(resourceName))
            {
                _resourcesStartedTcs.TrySetException(new DistributedApplicationException(message));
            }
            _waitingToStopResources.Remove(resourceName);
            _stoppedResources.Add(resourceName);
        }

        void HandleResourceStopError(string resourceName, string message)
        {
            _waitingToStartResources.Remove(resourceName);
            if (_waitingToStopResources.Remove(resourceName))
            {
                _resourcesStoppedTcs.TrySetException(new DistributedApplicationException(message));
            }
            _stoppedResources.Add(resourceName);
        }

        void HandleResourceStarted(IResource resource, string? suffix = null)
        {
            if (_waitingToStartResources.Remove(resource.Name) && _startedResources.Add(resource.Name))
            {
                logger.LogInformation($"Resource '{{resourceName}}' started{suffix}", resource.Name);
            }

            if (_waitingToStartResources.Count > 0)
            {
                var resourceNames = string.Join(", ", _waitingToStartResources.Select(r => r));
                logger.LogInformation("Still waiting on {resourcesToStartCount} resources to start: {resourcesToStart}", _waitingToStartResources.Count, resourceNames);
            }
        }

        void HandleResourceStopped(IResource resource, string? suffix = null)
        {
            if (_waitingToStopResources.Remove(resource.Name) && _stoppedResources.Add(resource.Name))
            {
                logger.LogInformation($"Resource '{{resourceName}}' stopped{suffix}", resource.Name);
            }

            if (_waitingToStopResources.Count > 0)
            {
                var resourceNames = string.Join(", ", _waitingToStopResources.Select(r => r));
                logger.LogInformation("Still waiting on {resourcesToStartCount} resources to stop: {resourcesToStart}", _waitingToStopResources.Count, resourceNames);
            }
        }

        await Task.WhenAll(logWatchTasks);
    }

    private async Task WatchResourceLogs(ResourceLogStore? logStore, ITestOutputHelper? testOutputHelper, IResource resource, string resourceId)
    {
        if (logStore is not null || testOutputHelper is not null)
        {
            await foreach (var logEvent in resourceLoggerService.WatchAsync(resourceId))
            {
                logStore?.Add(resource, logEvent);

                foreach (var line in logEvent)
                {
                    var kind = line.IsErrorMessage ? "error" : "log";
                    testOutputHelper?.WriteLine("{0} Resource '{1}' {2}: {3}", DateTime.Now.ToString("O"), resource.Name, kind, line.Content);
                }
            }
        }
    }

    private IEnumerable<IResource> GetStatusWatchableResources()
    {
        return appModel.Resources.Where(r => r is ContainerResource || r is ExecutableResource || r is ProjectResource || r is AzureConstructResource);
    }
}
