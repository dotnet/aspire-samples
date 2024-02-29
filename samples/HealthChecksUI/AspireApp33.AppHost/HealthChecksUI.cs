using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting;

public static class HealthChecksUIDefaults
{
    public const string ContainerImageName = "xabarilcoding/healthchecksui";
    public const int ContainerPort = 80;
    public const string ProbePath = "/healthz";
    public const string InternalEndpointName = "internalhealthchecks";
}

public static class HealthChecksUIEnvVars
{
    public const string InternalListenScheme = "INTERNAL_HEALTHCHECKS_LISTEN_SCHEME";
    public const string InternalListenHost = "INTERNAL_HEALTHCHECKS_LISTEN_HOST";
    public const string InternalListenPort = "INTERNAL_HEALTHCHECKS_LISTEN_PORT";
    public const string InternalPort = "INTERNAL_HEALTHCHECKS_PORT";
    public const string InternalPath = "INTERNAL_HEALTHCHECKS_PATH";
    public const string UiPath = "ui_path";
    public const string RootConfigurationKeyPrefix = "HealthChecksUI__";
    public const string HealthChecksConfigurationKeyPrefix = RootConfigurationKeyPrefix + "HealthChecks__";
    public const string HealthCheckConfigurationName = "Name";
    public const string HealthCheckConfigurationUri = "Uri";
}

public static class HealthChecksUIExtensions
{
    public static IResourceBuilder<HealthChecksUIContainerResource> AddHealthChecksUI(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        string? tag = null)
    {
        builder.Services.TryAddLifecycleHook<HealthChecksUILifecycleHook>();

        var keycloakContainer = new HealthChecksUIContainerResource(name);

        return builder
            .AddResource(keycloakContainer)
            .WithAnnotation(new ContainerImageAnnotation { Image = HealthChecksUIDefaults.ContainerImageName, Tag = tag ?? "latest" })
            .WithEnvironment(HealthChecksUIEnvVars.UiPath, "/")
            .WithHttpEndpoint(hostPort: port, containerPort: HealthChecksUIDefaults.ContainerPort);
    }

    public static IResourceBuilder<HealthChecksUIContainerResource> WithReference(
        this IResourceBuilder<HealthChecksUIContainerResource> builder,
        IResourceBuilder<ProjectResource> project,
        string probePath = HealthChecksUIDefaults.ProbePath,
        string endpointName = HealthChecksUIDefaults.InternalEndpointName,
        int? hostPort = null)
    {
        var healthCheck = new HealthCheck(project, endpointName: endpointName, probePath: probePath) { Port = hostPort };
        if (probePath is not null)
        {
            healthCheck.ProbePath = probePath;
        }
        builder.Resource.HealthChecks.Add(healthCheck);

        return builder;
    }
}

public class HealthChecksUIContainerResource(string name) : ContainerResource(name), IResourceWithServiceDiscovery
{
    public IList<HealthCheck> HealthChecks { get; } = [];
}

public class HealthCheck(IResourceBuilder<ProjectResource> project, string endpointName, string probePath)
{
    private string? _name;
    private Uri? _uri;

    public IResourceBuilder<ProjectResource> Project { get; } = project;

    public string EndpointName { get; } = endpointName;

    public string Name
    {
        get => _name ?? Project.Resource.Name;
        set
        {
            _name = value;
        }
    }

    public int? Port { get; set; }

    public string ProbePath { get; set; } = probePath;

    public Uri Uri
    {
        get
        {
            if (_uri is null)
            {
                var baseUri = new Uri(Project.GetEndpoint(EndpointName).Value);
                var healthChecksUri = new Uri(baseUri, ProbePath);
                _uri = healthChecksUri;
            }

            return _uri;
        }
    }
}

internal class HealthChecksUILifecycleHook : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        // Configure each project referenced by a Health Checks UI resource to listen on configured endpoint for health checks for the UI
        var healthChecksUIResources = appModel.Resources.OfType<HealthChecksUIContainerResource>();

        foreach (var healthChecksUIResource in healthChecksUIResources)
        {
            var healthChecks = healthChecksUIResource.HealthChecks;

            foreach (var healthCheck in healthChecks)
            {
                var project = healthCheck.Project;

                var healthChecksEndpoint = project.Resource.Annotations
                    .OfType<EndpointAnnotation>()
                    .SingleOrDefault(a => string.Equals(a.Name, healthCheck.EndpointName, StringComparison.OrdinalIgnoreCase));

                if (healthChecksEndpoint is null)
                {
                    // Add an endpoint for health checks if not already present
                    // TODO: Figure out HTTP vs. HTTPS
                    project.WithHttpEndpoint(hostPort: healthCheck.Port, name: healthCheck.EndpointName);
                }

                // Set the environment variable for the port of the health checks endpoint
                project.WithEnvironment(HealthChecksUIEnvVars.InternalPort, () =>
                {
                    if (project.Resource.TryGetAllocatedEndPoints(out var endpoints)
                        && endpoints.SingleOrDefault(e => string.Equals(e.Name, healthCheck.EndpointName, StringComparison.OrdinalIgnoreCase)) is { } allocatedEndpoint)
                    {
                        return allocatedEndpoint.Port.ToString();
                    }

                    throw new InvalidOperationException($"Couldn't find endpoint with name '{healthCheck.EndpointName}' for health checks.");
                });
            }
        }

        return Task.CompletedTask;
    }

    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var healhChecksUIResources = appModel.Resources.OfType<HealthChecksUIContainerResource>();

        // Add environment variables pointing to health checks endpoints of each referenced project on each Health Checks UI resource
        foreach (var healthChecksUIResource in healhChecksUIResources)
        {
            var healthChecks = healthChecksUIResource.HealthChecks;

            for (var i = 0; i < healthChecks.Count; i++)
            {
                var healthCheck = healthChecks[i];

                healthChecksUIResource.Annotations.Add(
                    new EnvironmentCallbackAnnotation($"{HealthChecksUIEnvVars.HealthChecksConfigurationKeyPrefix}{i}__{HealthChecksUIEnvVars.HealthCheckConfigurationName}",
                    () => healthCheck.Name));
                healthChecksUIResource.Annotations.Add(
                    new EnvironmentCallbackAnnotation($"{HealthChecksUIEnvVars.HealthChecksConfigurationKeyPrefix}{i}__{HealthChecksUIEnvVars.HealthCheckConfigurationUri}",
                    () => healthCheck.Uri.ToString().Replace("localhost", "host.docker.internal")));
            }
        }

        return Task.CompletedTask;
    }
}
