// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Aspire.Hosting.JavaScript;
using Aspire.Hosting.Python;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using SamplesIntegrationTests.Infrastructure;

namespace SamplesIntegrationTests.Infrastructure;

public static partial class DistributedApplicationExtensions
{
    private const string TestVolumePrefix = "samplesit-";
    private static readonly TimeSpan DockerVolumeCleanupTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Ensures all parameters in the application configuration have values set.
    /// </summary>
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
    /// Sets the container lifetime for all container resources in the application.
    /// </summary>
    public static TBuilder WithContainersLifetime<TBuilder>(this TBuilder builder, ContainerLifetime containerLifetime)
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        var containerLifetimeAnnotations = builder.Resources.SelectMany(r => r.Annotations
            .OfType<ContainerLifetimeAnnotation>()
            .Where(c => c.Lifetime != containerLifetime))
            .ToList();

        foreach (var annotation in containerLifetimeAnnotations)
        {
            annotation.Lifetime = containerLifetime;
        }

        return builder;
    }

    /// <summary>
    /// Replaces all named volumes with randomized names so they're isolated across test runs and from the volume the app uses during development.
    /// </summary>
    /// <remarks>
    /// Note that if multiple resources share a volume, they are all assigned the same randomized volume name so the volume remains shared in the test run.
    /// </remarks>
    public static TBuilder WithRandomVolumeNames<TBuilder>(this TBuilder builder)
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        // Named volumes shared by mulitple resources need to have their name randomized but kept shared across those resources.

        // Find all shared volumes and make a map of their original name to a new randomized name
        var allResourceNamedVolumes = builder.Resources.SelectMany(r => r.Annotations
            .OfType<ContainerMountAnnotation>()
            .Where(m => m.Type == ContainerMountType.Volume && !string.IsNullOrEmpty(m.Source))
            .Select(m => (Resource: r, Volume: m)))
            .ToList();
        var seenVolumes = new HashSet<string>();
        var renamedVolumes = new Dictionary<string, string>();
        foreach (var resourceVolume in allResourceNamedVolumes)
        {
            var name = resourceVolume.Volume.Source!;
            if (!seenVolumes.Add(name) && !renamedVolumes.ContainsKey(name))
            {
                renamedVolumes[name] = CreateRandomizedVolumeName(name);
            }
        }

        // Replace all named volumes with randomized names.
        foreach (var resourceVolume in allResourceNamedVolumes)
        {
            var resource = resourceVolume.Resource;
            var volume = resourceVolume.Volume;
            var newName = renamedVolumes.TryGetValue(volume.Source!, out var randomName)
                ? randomName
                : CreateRandomizedVolumeName(volume.Source!);
            var newMount = new ContainerMountAnnotation(newName, volume.Target, ContainerMountType.Volume, volume.IsReadOnly);
            resource.Annotations.Remove(volume);
            resource.Annotations.Add(newMount);
        }

        return builder;
    }

    public static async Task CleanupRandomizedVolumesAsync(this DistributedApplication app, Action<string>? log = null, CancellationToken cancellationToken = default)
    {
        var volumeNames = app.Services.GetRequiredService<DistributedApplicationModel>()
            .Resources
            .SelectMany(r => r.Annotations.OfType<ContainerMountAnnotation>())
            .Where(m => m.Type == ContainerMountType.Volume
                && !string.IsNullOrEmpty(m.Source)
                && m.Source.StartsWith(TestVolumePrefix, StringComparison.Ordinal))
            .Select(m => m.Source!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var volumeName in volumeNames)
        {
            await TryRemoveDockerVolumeAsync(volumeName, log, cancellationToken);
        }
    }

    /// <summary>
    /// Waits for the specified resource to reach the specified state.
    /// </summary>
    public static Task WaitForResource(this DistributedApplication app, string resourceName, string? targetState = null, CancellationToken cancellationToken = default)
    {
        targetState ??= KnownResourceStates.Running;
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();

        return resourceNotificationService.WaitForResourceAsync(resourceName, targetState, cancellationToken);
    }

    /// <summary>
    /// Waits for all resources in the application to reach one of the specified states.
    /// </summary>
    /// <remarks>
    /// If <paramref name="targetStates"/> is null, the default states are <see cref="KnownResourceStates.Running"/> and <see cref="KnownResourceStates.Hidden"/>.
    /// </remarks>
    public static async Task WaitForResourcesAsync(this DistributedApplication app, IEnumerable<string>? targetStates = null, CancellationToken cancellationToken = default)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger($"{nameof(SamplesIntegrationTests)}.{nameof(WaitForResourcesAsync)}");

        targetStates ??= [KnownResourceStates.Running, ..KnownResourceStates.TerminalStates];
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resourceTasks = new Dictionary<string, Task<(string Name, string State)>>();

        foreach (var resource in applicationModel.Resources)
        {
            var explicitStartup = resource.Annotations.OfType<ExplicitStartupAnnotation>().FirstOrDefault();
            if (resource is IResourceWithoutLifetime || explicitStartup is not null)
            {
                continue;
            }
            resourceTasks[resource.Name] = GetResourceWaitTask(resource.Name, targetStates, cancellationToken);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Waiting for resources [{Resources}] to reach one of target states [{TargetStates}].",
                string.Join(',', resourceTasks.Keys),
                string.Join(',', targetStates));
        }

        while (resourceTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(resourceTasks.Values);
            var (completedResourceName, targetStateReached) = await completedTask;

            if (targetStateReached == KnownResourceStates.FailedToStart)
            {
                throw new DistributedApplicationException($"Resource '{completedResourceName}' failed to start.");
            }

            resourceTasks.Remove(completedResourceName);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Wait for resource '{ResourceName}' completed with state '{ResourceState}'", completedResourceName, targetStateReached);
            }

            // Ensure resources being waited on still exist
            var remainingResources = resourceTasks.Keys.ToList();
            for (var i = remainingResources.Count - 1; i > 0; i--)
            {
                var name = remainingResources[i];
                if (!applicationModel.Resources.Any(r => r.Name == name))
                {
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Resource '{ResourceName}' was deleted while waiting for it.", name);
                    }
                    resourceTasks.Remove(name);
                    remainingResources.RemoveAt(i);
                }
            }

            if (resourceTasks.Count > 0)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Still waiting for resources [{Resources}] to reach one of target states [{TargetStates}].",
                        string.Join(',', remainingResources),
                        string.Join(',', targetStates));
                }
            }
        }

        logger.LogInformation("Wait for all resources completed successfully!");

        async Task<(string Name, string State)> GetResourceWaitTask(string resourceName, IEnumerable<string> targetStates, CancellationToken cancellationToken)
        {
            var state = await app.ResourceNotifications.WaitForResourceAsync(resourceName, targetStates, cancellationToken);
            return (resourceName, state);
        }
    }

    /// <summary>
    /// Gets the app host and resource logs from the application.
    /// </summary>
    public static (IReadOnlyList<FakeLogRecord> AppHostLogs, IReadOnlyList<FakeLogRecord> ResourceLogs) GetLogs(this DistributedApplication app)
    {
        var environment = app.Services.GetRequiredService<IHostEnvironment>();
        var logCollector = app.Services.GetFakeLogCollector();
        var logs = logCollector.GetSnapshot();
        var appHostLogs = logs.Where(l => l.Category?.StartsWith($"{environment.ApplicationName}.Resources") == false).ToList();
        var resourceLogs = logs.Where(l => l.Category?.StartsWith($"{environment.ApplicationName}.Resources") == true).ToList();

        return (appHostLogs, resourceLogs);
    }

    /// <summary>
    /// Asserts that no errors were logged by the application or any of its resources.
    /// </summary>
    /// <remarks>
    /// Some resource types are excluded from this check because they tend to write to stderr for various non-error reasons.
    /// </remarks>
    /// <param name="app"></param>
    public static void EnsureNoErrorsLogged(this DistributedApplication app)
    {
        var environment = app.Services.GetRequiredService<IHostEnvironment>();
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var assertableResourceLogNames = applicationModel.Resources.Where(ShouldAssertErrorsForResource).Select(r => $"{environment.ApplicationName}.Resources.{r.Name}").ToList();

        var (appHostlogs, resourceLogs) = app.GetLogs();

        Assert.DoesNotContain(appHostlogs, log => log.Level >= LogLevel.Error);
        Assert.DoesNotContain(resourceLogs, log => log.Category is { Length: > 0 } category && assertableResourceLogNames.Contains(category) && log.Level >= LogLevel.Error);

        static bool ShouldAssertErrorsForResource(IResource resource)
        {
#pragma warning disable ASPIREHOSTINGPYTHON001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            return resource
                is
                    // Container resources tend to write to stderr for various reasons so only assert projects and executables
                    (ProjectResource or ExecutableResource)
                    // Node & Python resources tend to have modules that write to stderr so ignore them
                    and not (NodeAppResource or PythonAppResource);
#pragma warning restore ASPIREHOSTINGPYTHON001
        }
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource.
    /// </summary>
    public static HttpClient CreateHttpClient(this DistributedApplication app, string resourceName, bool useHttpClientFactory)
        => app.CreateHttpClient(resourceName, null, useHttpClientFactory);

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource.
    /// </summary>
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
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource with custom configuration.
    /// </summary>
    public static HttpClient CreateHttpClient(this DistributedApplication app, string resourceName, string? endpointName, Action<IHttpClientBuilder> configure)
    {
        var services = new ServiceCollection()
            .AddHttpClient()
            .ConfigureHttpClientDefaults(configure)
            .BuildServiceProvider();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = app.GetEndpoint(resourceName, endpointName);

        return httpClient;
    }

    /// <summary>
    /// Attempts to apply EF migrations for the specified project by sending a request to the migrations endpoint <c>/ApplyDatabaseMigrations</c>.
    /// </summary>
    public static async Task<bool> TryApplyEfMigrationsAsync(this DistributedApplication app, ProjectResource project)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(TryApplyEfMigrationsAsync));
        var projectName = project.GetName();

        // First check if the project has a migration endpoint, if it doesn't it will respond with a 404
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Checking if project '{ProjectName}' has a migration endpoint", projectName);
        }
        using (var checkHttpClient = app.CreateHttpClient(project.Name))
        {
            using var emptyDbContextContent = new FormUrlEncodedContent([new("context", "")]);
            using var checkResponse = await checkHttpClient.PostAsync("/ApplyDatabaseMigrations", emptyDbContextContent);
            if (checkResponse.StatusCode == HttpStatusCode.NotFound)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Project '{ProjectName}' does not have a migration endpoint", projectName);
                }
                return false;
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Attempting to apply EF migrations for project '{ProjectName}'", projectName);
        }

        // Load the project assembly and find all DbContext types
        var projectDirectory = Path.GetDirectoryName(project.GetProjectMetadata().ProjectPath) ?? throw new UnreachableException();
#if DEBUG
        var configuration = "Debug";
#else
        var configuration = "Release";
#endif
        var projectAssemblyPath = Path.Combine(projectDirectory, "bin", configuration, "net10.0", $"{projectName}.dll");
        var projectAssembly = Assembly.LoadFrom(projectAssemblyPath);
        var dbContextTypes = projectAssembly.GetTypes().Where(t => DerivesFromDbContext(t));

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Found {DbContextCount} DbContext types in project '{ProjectName}'", dbContextTypes.Count(), projectName);
        }

        // Call the migration endpoint for each DbContext type
        var migrationsApplied = false;
        using var applyMigrationsHttpClient = app.CreateHttpClient(project.Name, useHttpClientFactory: false);
        applyMigrationsHttpClient.Timeout = TimeSpan.FromSeconds(240);
        foreach (var dbContextType in dbContextTypes)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Applying migrations for DbContext '{DbContextType}' in project '{ProjectName}'", dbContextType.FullName, projectName);
            }
            using var content = new FormUrlEncodedContent([new("context", dbContextType.AssemblyQualifiedName)]);
            using var response = await applyMigrationsHttpClient.PostAsync("/ApplyDatabaseMigrations", content);
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                migrationsApplied = true;
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Migrations applied for DbContext '{DbContextType}' in project '{ProjectName}'", dbContextType.FullName, projectName);
                }
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

    private static string CreateRandomizedVolumeName(string sourceVolumeName)
        => $"{TestVolumePrefix}{sourceVolumeName}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";

    private static async Task TryRemoveDockerVolumeAsync(string volumeName, Action<string>? log, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("volume");
        process.StartInfo.ArgumentList.Add("rm");
        process.StartInfo.ArgumentList.Add("-f");
        process.StartInfo.ArgumentList.Add(volumeName);

        try
        {
            if (!process.Start())
            {
                log?.Invoke($"Failed to start docker process while cleaning volume '{volumeName}'.");
                return;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(DockerVolumeCleanupTimeout);

            var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                log?.Invoke($"Timed out cleaning docker volume '{volumeName}' after {DockerVolumeCleanupTimeout.TotalSeconds:0} seconds.");
                TryKillProcess(process, log, volumeName);
                return;
            }

            var stdOut = (await stdOutTask).Trim();
            var stdErr = (await stdErrTask).Trim();

            if (process.ExitCode != 0)
            {
                log?.Invoke($"Docker volume cleanup for '{volumeName}' exited with code {process.ExitCode}. stdout: {stdOut} stderr: {stdErr}");
                return;
            }

            if (!string.IsNullOrEmpty(stdErr))
            {
                log?.Invoke($"Docker volume cleanup warning for '{volumeName}': {stdErr}");
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"Docker volume cleanup failed for '{volumeName}': {ex.Message}");
        }
    }

    private static void TryKillProcess(Process process, Action<string>? log, string volumeName)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"Failed to terminate docker cleanup process for volume '{volumeName}': {ex.Message}");
        }
    }
}
