using Aspire.Hosting.Lifecycle;

namespace HealthChecksUI;

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
                    // TODO: Figure out HTTP vs. HTTPS, e.g. find other endpoints added and if there's an HTTPS endpoint then make this one HTTPS too
                    project.WithHttpEndpoint(hostPort: healthCheck.Port, name: healthCheck.EndpointName);
                }

                // Set the environment variables for the port and probe path of the health checks endpoint
                project.WithEnvironment(HealthChecksUIEnvVars.InternalUrl, () =>
                {
                    if (project.Resource.TryGetAllocatedEndPoints(out var endpoints)
                        && endpoints.SingleOrDefault(e => string.Equals(e.Name, healthCheck.EndpointName, StringComparison.OrdinalIgnoreCase)) is { } allocatedEndpoint)
                    {
                        var baseUri = new Uri(allocatedEndpoint.UriString);
                        var fullUri = new Uri(baseUri, healthCheck.ProbePath);

                        return fullUri.ToString();
                        //return allocatedEndpoint.Port.ToString();
                    }

                    throw new InvalidOperationException($"Couldn't find endpoint with name '{healthCheck.EndpointName}' for health checks.");
                });
                //project.WithEnvironment(HealthChecksUIEnvVars.InternalPath, () => healthCheck.ProbePath);
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
