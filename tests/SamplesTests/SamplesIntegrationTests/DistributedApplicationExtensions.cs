using System.Text;
using Aspire.Hosting.Utils;
using IdentityModel;
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

        builder.Services.AddHostedService(sp => new ResourceWatcher(
            sp.GetRequiredService<DistributedApplicationModel>(),
            sp.GetRequiredService<ResourceNotificationService>(),
            sp.GetRequiredService<ResourceLoggerService>(),
            testOutputHelper));

        return builder;
    }

    public static TBuilder WithRandomParameterValues<TBuilder>(this TBuilder builder)
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        var parameters = builder.Resources.OfType<ParameterResource>().Where(p => !p.IsConnectionString).ToList();
        foreach (var parameter in parameters)
        {
            builder.Configuration[$"Parameters:{parameter.Name}"] = PasswordGenerator.Generate(16, true, true, true, false, 1, 1, 1, 0);
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

    private class ResourceWatcher(DistributedApplicationModel applicationModel, ResourceNotificationService resourceNotification, ResourceLoggerService resourceLoggerService, ITestOutputHelper testOutputHelper)
        : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.WhenAll(WatchNotifications(stoppingToken), WatchLogs(stoppingToken));
        }

        private async Task WatchNotifications(CancellationToken stoppingToken)
        {
            await foreach (var resourceEvent in resourceNotification.WatchAsync().WithCancellation(stoppingToken))
            {
                if (resourceEvent.Snapshot is { } snapshot)
                {
                    if (snapshot.ExitCode is null && snapshot.State is { } state)
                    {
                        testOutputHelper.WriteLine("Resource '{0}' state: {1}", resourceEvent.ResourceId, state.Text);
                    }
                }
            }
        }

        private Task WatchLogs(CancellationToken stoppingToken)
        {
            var logTasks = applicationModel.Resources.Select(r => WatchLogs(r, stoppingToken));
            return Task.WhenAll(logTasks);
        }

        private async Task WatchLogs(IResource resource, CancellationToken stoppingToken)
        {
            await foreach (var logEvent in resourceLoggerService.WatchAsync(resource).WithCancellation(stoppingToken))
            {
                foreach (var line in logEvent)
                {
                    var kind = line.IsErrorMessage ? "error" : "log";
                    testOutputHelper.WriteLine("Resource '{0}' {1}: {2}", resource.Name, kind, line.Content);
                }
            }
        }
    }

    private class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;
        private readonly LoggerExternalScopeProvider _scopeProvider;

        public static ILogger CreateLogger(ITestOutputHelper testOutputHelper) => new XUnitLogger(testOutputHelper, new LoggerExternalScopeProvider(), "");
        public static ILogger<T> CreateLogger<T>(ITestOutputHelper testOutputHelper) => new XUnitLogger<T>(testOutputHelper, new LoggerExternalScopeProvider());

        public XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _scopeProvider = scopeProvider;
            _categoryName = categoryName;
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _scopeProvider.Push(state);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var sb = new StringBuilder();
            sb.Append(GetLogLevelString(logLevel))
              .Append(" [").Append(_categoryName).Append("] ")
              .Append(formatter(state, exception));

            if (exception != null)
            {
                sb.Append('\n').Append(exception);
            }

            // Append scopes
            _scopeProvider.ForEachScope((scope, state) =>
            {
                state.Append("\n => ");
                state.Append(scope);
            }, sb);

            _testOutputHelper.WriteLine(sb.ToString());
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

    private sealed class XUnitLogger<T> : XUnitLogger, ILogger<T>
    {
        public XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider)
            : base(testOutputHelper, scopeProvider, typeof(T).FullName ?? "")
        {
        }
    }

    private sealed class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly LoggerExternalScopeProvider _scopeProvider = new ();

        public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_testOutputHelper, _scopeProvider, categoryName);
        }

        public void Dispose()
        {
        }
    }
}
