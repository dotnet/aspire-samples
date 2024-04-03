using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SamplesIntegrationTests;

public static partial class DistributedApplicationExtensions
{
    /// <summary>
    /// Adds a background service to watch resource status changes and optionally logs.
    /// </summary>
    public static IServiceCollection AddResourceWatching(this IServiceCollection services)
    {
        // Add background service to watch resource status changes and optionally logs
        services.AddSingleton<ResourceWatcher>();
        services.AddHostedService(sp => sp.GetRequiredService<ResourceWatcher>());

        return services;
    }

    /// <summary>
    /// Configures the builder to write logs to xunit's output and store for optional assertion later.
    /// </summary>
    public static TBuilder WriteOutputTo<TBuilder>(this TBuilder builder, ITestOutputHelper testOutputHelper)
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        builder.Services.AddResourceWatching();

        // Add a resource log store to capture logs from resources
        builder.Services.AddSingleton<ResourceLogStore>();

        // Configure the builder's logger to redirect it to xunit's output & store for assertion later
        builder.Services.AddLogging(logging => logging.ClearProviders());
        builder.Services.AddSingleton(testOutputHelper);
        builder.Services.AddSingleton<ILoggerProvider, XUnitLoggerProvider>();
        builder.Services.AddSingleton<LoggerLogStore>();
        builder.Services.AddSingleton<ILoggerProvider, StoredLogsLoggerProvider>();

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
        var resourceWatcher = app.Services.GetRequiredService<ResourceWatcher>();
        var resourcesStartingTask = waitForResourcesToStart ? resourceWatcher.WaitForResourcesToStart() : Task.CompletedTask;

        await app.StartAsync(cancellationToken);
        await resourcesStartingTask;
    }

    /// <inheritdoc cref = "IHost.StopAsync" />
    public static async Task StopAsync(this DistributedApplication app, bool waitForResourcesToStop, TimeSpan? waitForResourcesTimeout = null, CancellationToken cancellationToken = default)
    {
        var resourceWatcher = app.Services.GetRequiredService<ResourceWatcher>();
        var resourcesStoppingTask = waitForResourcesToStop ? resourceWatcher.WaitForResourcesToStop() : Task.CompletedTask;

        await app.StopAsync(cancellationToken);

        waitForResourcesTimeout ??= TimeSpan.FromSeconds(60);
        var stopTimeoutCts = new CancellationTokenSource(waitForResourcesTimeout.Value);
        var stopTimeoutTcs = new TaskCompletionSource();
        stopTimeoutCts.Token.Register(() => stopTimeoutTcs.TrySetException(new DistributedApplicationException($"Resources did not stop within the configured timeout {waitForResourcesTimeout}")));
        
        await (await Task.WhenAny(resourcesStoppingTask, stopTimeoutTcs.Task));
    }

    public static LoggerLogStore GetAppHostLogs(this DistributedApplication app)
    {
        var logStore = app.Services.GetService<LoggerLogStore>()
            ?? throw new InvalidOperationException($"Log store service was not registered. Ensure the '{nameof(WriteOutputTo)}' method is called before attempting to get AppHost logs.");
        return logStore;
    }

    /// <summary>
    /// Gets the logs for all resources in the application.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static ResourceLogStore GetResourceLogs(this DistributedApplication app)
    {
        var logStore = app.Services.GetService<ResourceLogStore>()
            ?? throw new InvalidOperationException($"Log store service was not registered. Ensure the '{nameof(WriteOutputTo)}' method is called before attempting to get resource logs."); ;
        return logStore;
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

    //private static readonly TimeSpan resourceStartDefaultTimeout = TimeSpan.FromSeconds(30);

    //private static async Task WaitForResourcesToStartAsync(DistributedApplication app, CancellationToken cancellationToken = default)
    //{
    //    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DistributedApplicationExtensions));
    //    var resources = GetWaitableResources(app).ToList();
    //    var remainingResources = new HashSet<IResource>(resources);

    //    logger.LogInformation("Waiting on {resourcesToStartCount} resources to start", remainingResources.Count);

    //    var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();
    //    var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

    //    var timeoutCts = new CancellationTokenSource(resourceStartDefaultTimeout);
    //    var cts = cancellationToken == default
    //        ? timeoutCts
    //        : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

    //    await foreach (var resourceEvent in notificationService.WatchAsync().WithCancellation(cts.Token))
    //    {
    //        var resourceName = resourceEvent.Resource.Name;
    //        if (remainingResources.Contains(resourceEvent.Resource))
    //        {
    //            // TODO: Handle replicas
    //            var snapshot = resourceEvent.Snapshot;
    //            if (snapshot.ExitCode is { } exitCode)
    //            {
    //                if (exitCode != 0)
    //                {
    //                    // Error starting resource
    //                    throw new DistributedApplicationException($"Resource '{resourceName}' exited with exit code {exitCode}");
    //                }

    //                // Resource exited cleanly
    //                HandleResourceStarted(resourceEvent.Resource, " (exited with code 0)");
    //            }
    //            else if (snapshot.State is { } state)
    //            {
    //                if (state.Text.Contains("running", StringComparison.OrdinalIgnoreCase))
    //                {
    //                    // Resource started
    //                    HandleResourceStarted(resourceEvent.Resource);
    //                }
    //                else if (state.Text.Contains("failedtostart", StringComparison.OrdinalIgnoreCase))
    //                {
    //                    // Resource failed to start
    //                    throw new DistributedApplicationException($"Resource '{resourceName}' failed to start: {state.Text}");
    //                }
    //                else if (state.Text.Contains("exited", StringComparison.OrdinalIgnoreCase) && remainingResources.Contains(resourceEvent.Resource))
    //                {
    //                    // Resource went straight to exited state
    //                    throw new DistributedApplicationException($"Resource '{resourceName}' exited without first running: {state.Text}");
    //                }
    //                else if (state.Text.Contains("starting", StringComparison.OrdinalIgnoreCase)
    //                         || state.Text.Contains("hidden", StringComparison.OrdinalIgnoreCase))
    //                {
    //                    // Resource is still starting
    //                    continue;
    //                }
    //                else if (!string.IsNullOrEmpty(state.Text))
    //                {
    //                    logger.LogWarning("Unknown resource state encountered: {state}", state.Text);
    //                }
    //            }
    //        }

    //        if (remainingResources.Count == 0)
    //        {
    //            logger.LogInformation("All resources started successfully");
    //            break;
    //        }
    //    }

    //    void HandleResourceStarted(IResource resource, string? suffix = null)
    //    {
    //        remainingResources.Remove(resource);
    //        logger.LogInformation($"Resource '{{resourceName}}' started{suffix}", resource.Name);
    //        if (remainingResources.Count > 0)
    //        {
    //            var resourceNames = string.Join(", ", remainingResources.Select(r => r.Name));
    //            logger.LogInformation("Still waiting on {resourcesToStartCount} resources to start: {resourcesToStart}", remainingResources.Count, resourceNames);
    //        }
    //    }
    //}

    //private static IEnumerable<IResource> GetWaitableResources(this DistributedApplication app)
    //{
    //    var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
    //    return appModel.Resources.Where(r => r is ContainerResource || r is ExecutableResource || r is ProjectResource || r is AzureConstructResource);
    //}
}
