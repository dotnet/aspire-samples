using System.Collections.Concurrent;
using System.Reflection;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace SamplesIntegrationTests.Infrastructure;

internal class AutoRestartResourceLifecycleHook(
    IOptions<AutoRestartOptions> options,
    IHostEnvironment environment,
    ResourceNotificationService resourceNotificationService,
    FakeLogCollector fakeLogCollector,
    IServiceProvider serviceProvider)
    : IDistributedApplicationLifecycleHook
{
    private readonly ApplicationExecutorProxy _applicationExecutorProxy = new(serviceProvider.GetRequiredService("ApplicationExecutor", typeof(DistributedApplication).Assembly));
    private readonly ConcurrentDictionary<string, int> _resourceRestartAttempts = new();

    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        Task.Run(() => WatchResourceUpdates(cancellationToken), cancellationToken);

        return Task.CompletedTask;
    }

    private async Task WatchResourceUpdates(CancellationToken cancellationToken)
    {
        await foreach (var resourceEvent in resourceNotificationService.WatchAsync(cancellationToken))
        {
            if (KnownResourceStates.TerminalStates.Contains(resourceEvent.Snapshot.State?.Text)
                && resourceEvent.Snapshot.ExitCode is not 0
                && ResourceLogsContainsMessage(resourceEvent.Resource.Name, options.Value.LogMessages))
            {
                StartResource(resourceEvent.Resource.Name, cancellationToken);
            }
        }
    }

    private void StartResource(string resourceName, CancellationToken cancellationToken)
    {
        var attemptCount = _resourceRestartAttempts.GetOrAdd(resourceName, 0);
        if (attemptCount >= options.Value.Attempts)
        {
            return;
        }
        _applicationExecutorProxy.StartResourceAsync(resourceName, cancellationToken);
        _resourceRestartAttempts.AddOrUpdate(resourceName, 1, (_, count) => count + 1);
    }

    private bool ResourceLogsContainsMessage(string resourceName, List<string> logMessages)
    {
        var logs = fakeLogCollector.GetSnapshot();
        return logs.Any(l => l.Category?.StartsWith($"{environment.ApplicationName}.Resources.{resourceName}") == true
                             && logMessages.Any(m => l.Message.Contains(m)));
    }

    private class ApplicationExecutorProxy
    {
        private readonly object _executor;
        private readonly Func<string, CancellationToken, Task> _startResourceAsync;
        private readonly Func<string, CancellationToken, Task> _stopResourceAsync;

        public ApplicationExecutorProxy(object applicationExecutor)
        {
            _executor = applicationExecutor;

            var startResourceAsyncMethodInfo = applicationExecutor.GetType()
                .GetMethod(nameof(StartResourceAsync), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(string), typeof(CancellationToken)])
                ?? throw new InvalidOperationException("Method not found.");
            _startResourceAsync = startResourceAsyncMethodInfo.CreateDelegate<Func<string, CancellationToken, Task>>(_executor);

            var stopResourceAsyncMethodInfo = applicationExecutor.GetType()
                .GetMethod(nameof(StopResourceAsync), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(string), typeof(CancellationToken)])
                ?? throw new InvalidOperationException("Method not found.");
            _stopResourceAsync = stopResourceAsyncMethodInfo.CreateDelegate<Func<string, CancellationToken, Task>>(_executor);
        }

        public Task StartResourceAsync(string resourceName, CancellationToken cancellationToken) => _startResourceAsync(resourceName, cancellationToken);

        public Task StopResourceAsync(string resourceName, CancellationToken cancellationToken) => _stopResourceAsync(resourceName, cancellationToken);
    }
}
