using Aspire.Hosting.Lifecycle;

namespace HealthChecksUI;

/// <summary>
/// A container-based resource for the HealthChecksUI container.
/// See https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks?tab=readme-ov-file#HealthCheckUI
/// </summary>
/// <param name="name">The resource name.</param>
public class HealthChecksUIResource(string name) : ContainerResource(name), IResourceWithServiceDiscovery
{
    public IList<MonitoredProject> MonitoredProjects { get; } = [];

    /// <summary>
    /// Known environment variables for the HealthChecksUI container that can be used to configure the container.
    /// Taken from https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/blob/master/doc/ui-docker.md#environment-variables-table
    /// </summary>
    public static class KnownEnvVars
    {
        public const string UiPath = "ui_path";
        // These keys are taken from https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks?tab=readme-ov-file#sample-2-configuration-using-appsettingsjson
        public const string HealthChecksConfigSection = "HealthChecksUI__HealthChecks";
        public const string HealthCheckName = "Name";
        public const string HealthCheckUri = "Uri";

        internal static string GetHealthCheckNameKey(int index) => $"{HealthChecksConfigSection}__{index}__{HealthCheckName}";

        internal static string GetHealthCheckUriKey(int index) => $"{HealthChecksConfigSection}__{index}__{HealthCheckUri}";
    }
}

/// <summary>
/// Represents a project to be monitored by a <see cref="HealthChecksUIResource"/>.
/// </summary>
public class MonitoredProject(IResourceBuilder<ProjectResource> project, string endpointName, string probePath)
{
    private string? _name;
    private Uri? _uri;
    private string? _uriExpression;

    /// <summary>
    /// The project to be monitored.
    /// </summary>
    public IResourceBuilder<ProjectResource> Project { get; } = project ?? throw new ArgumentNullException(nameof(project));

    /// <summary>
    /// The name of the endpoint the project serves health check details from. If it doesn't exist it will be added.
    /// </summary>
    public string EndpointName { get; } = endpointName ?? throw new ArgumentNullException(nameof(endpointName));

    /// <summary>
    /// The port to use if creating a new endpoint for the health checks.
    /// </summary>
    public int? EndpointPort { get; set; }

    public string Name
    {
        get => _name ?? Project.Resource.Name;
        set { _name = value; }
    }

    public string ProbePath { get; set; } = probePath ?? throw new ArgumentNullException(nameof(probePath));

    public Uri ProjectUri
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

    public string ProjectUriExpression
    {
        get
        {
            if (_uriExpression is null)
            {
                var baseUrl = Project.GetEndpoint(EndpointName).ValueExpression.TrimEnd('/');
                var path = ProbePath.TrimStart('/');
                _uriExpression = $"{baseUrl}/{path}";
            }

            return _uriExpression;
        }
    }
}

internal class HealthChecksUILifecycleHook(DistributedApplicationExecutionContext executionContext) : IDistributedApplicationLifecycleHook
{
    private const string HEALTHCHECKSUI_URLS = "HEALTHCHECKSUI_URLS";

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        // Configure each project referenced by a Health Checks UI resource to listen on configured endpoint for health checks for the UI
        var healthChecksUIResources = appModel.Resources.OfType<HealthChecksUIResource>();

        foreach (var healthChecksUIResource in healthChecksUIResources)
        {
            var healthChecks = healthChecksUIResource.MonitoredProjects;

            foreach (var healthCheck in healthChecks)
            {
                var project = healthCheck.Project;

                var existingHealthCheckEndpoint = project.Resource.GetEndpoint(healthCheck.EndpointName);

                if (existingHealthCheckEndpoint is null)
                {
                    // Add an endpoint for health checks if not already present.
                    // Use existing endpoints to figure out if project is configured for HTTPS or not (related to https://github.com/dotnet/aspire/issues/2453)
                    if (project.Resource.HasHttpsEndpoint())
                    {
                        project.WithHttpsEndpoint(hostPort: healthCheck.EndpointPort, name: healthCheck.EndpointName);
                    }
                    else if (project.Resource.HasHttpEndpoint())
                    {
                        project.WithHttpEndpoint(hostPort: healthCheck.EndpointPort, name: healthCheck.EndpointName);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Project resource '{project.Resource.Name}' has no HTTP or HTTPS endpoints so cannot be configured for HealthChecksUI.");
                    }
                }
                else if (healthCheck.EndpointPort.HasValue)
                {
                    throw new InvalidOperationException($"Cannot specify a custom port for health check endpoint for project'{project.Resource.Name}' as the endpoint name specified '{healthCheck.EndpointName}' already exists.");
                }

                project.WithEnvironment(context =>
                {
                    if (context.ExecutionContext.IsRunMode)
                    {
                        // Running during dev inner-loop
                        // Set/update the environment variable for health checks endpoint URL
                        if (project.Resource.GetAllocatedEndpoint(healthCheck.EndpointName) is { } allocatedEndpoint
                            && Uri.TryCreate(allocatedEndpoint.UriString, UriKind.Absolute, out var baseUri))
                        {
                            var healthChecksUri = new Uri(baseUri, healthCheck.ProbePath);

                            context.EnvironmentVariables.AddDelimitedValues(HEALTHCHECKSUI_URLS,
                                [healthChecksUri.ToString(), healthChecksUri.ToString().Replace("localhost", "host.docker.internal")]);

                            // Set the AllowedHosts environment variable to configure host filtering
                            context.EnvironmentVariables.AddDelimitedValues("AllowedHosts", [baseUri.Host, baseUri.Host.Replace("localhost", "host.docker.internal")]);

                            return;
                        }

                        throw new InvalidOperationException($"Couldn't find endpoint with name '{healthCheck.EndpointName}' for health checks or endpoint could not be parsed as a valid URI.");
                    }
                    else
                    {
                        // Publishing the manifest
                        // "{resourcename.bindings.endpoint.url}"
                        // Set/update the environment variable for health checks endpoint URL
                        context.EnvironmentVariables.AddDelimitedValue(HEALTHCHECKSUI_URLS,
                            $"{{{project.Resource.Name}.bindings.{healthCheck.EndpointName}.url}}/{healthCheck.ProbePath.TrimStart('/')}");

                        // Set the AllowedHosts environment variable to configure host filtering
                        // TODO: We probably want to do this by default in Aspire.Hosting
                        if (project.Resource.TryGetEndpoints(out var endpoints))
                        {
                            var hosts = endpoints.Select(e => $"{{{project.Resource.Name}.bindings.{e.Name}.host}}");
                            context.EnvironmentVariables.AddDelimitedValues("AllowedHosts", hosts);
                        }
                    }
                });
            }
        }

        if (executionContext.IsPublishMode)
        {
            ConfigureHealthChecksUIContainers(appModel.Resources, isPublishing: true);
        }

        return Task.CompletedTask;
    }

    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        ConfigureHealthChecksUIContainers(appModel.Resources, isPublishing: false);

        return Task.CompletedTask;
    }

    private static void ConfigureHealthChecksUIContainers(IResourceCollection resources, bool isPublishing)
    {
        var healhChecksUIResources = resources.OfType<HealthChecksUIResource>();

        foreach (var healthChecksUIResource in healhChecksUIResources)
        {
            var healthChecks = healthChecksUIResource.MonitoredProjects;

            // Add environment variables to configure the HealthChecksUI container with the health checks endpoints of each referenced project
            // See example configuration at https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks?tab=readme-ov-file#sample-2-configuration-using-appsettingsjson
            for (var i = 0; i < healthChecks.Count; i++)
            {
                var healthCheck = healthChecks[i];

                healthChecksUIResource.Annotations.Add(
                    new EnvironmentCallbackAnnotation(
                        HealthChecksUIResource.KnownEnvVars.GetHealthCheckNameKey(i),
                        () => healthCheck.Name));
                healthChecksUIResource.Annotations.Add(
                    new EnvironmentCallbackAnnotation(
                        HealthChecksUIResource.KnownEnvVars.GetHealthCheckUriKey(i),
                        () => isPublishing ? healthCheck.ProjectUriExpression : healthCheck.ProjectUri.ToString().Replace("localhost", "host.docker.internal")));
            }
        }
    }
}

internal static class ResourceExtensions
{
    public static EndpointAnnotation? GetEndpoint(this IResource resource, string name) =>
        resource.Annotations.OfType<EndpointAnnotation>().FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

    public static AllocatedEndpointAnnotation? GetAllocatedEndpoint(this IResource resource, string name) =>
        resource.Annotations.OfType<AllocatedEndpointAnnotation>().FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

    public static bool HasHttpEndpoint(this IResource resource) =>
        resource.TryGetEndpoints(out var endpoints) && endpoints.Any(e => string.Equals(e.UriScheme, "http", StringComparison.OrdinalIgnoreCase));

    public static bool HasHttpsEndpoint(this IResource resource) =>
        resource.TryGetEndpoints(out var endpoints) && endpoints.Any(e => string.Equals(e.UriScheme, "https", StringComparison.OrdinalIgnoreCase));

    public static void AddDelimitedValues(this IDictionary<string, string> dictionary, string key, IEnumerable<string> values, char separator = ';')
    {
        HashSet<string> existingValues = dictionary.TryGetValue(key, out var existing)
            ? new(existing.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            : [];
        foreach (var value in values)
        {
            existingValues.Add(value);
        }
        dictionary[key] = string.Join(separator, existingValues);
    }

    public static void AddDelimitedValue(this IDictionary<string, string> dictionary, string key, string value, char separator = ';')
    {
        HashSet<string> values = dictionary.TryGetValue(key, out var existing)
            ? new(existing.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            : [];
        values.Add(value);
        dictionary[key] = string.Join(separator, values);
    }
}
