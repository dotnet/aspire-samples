using Aspire.Hosting.Lifecycle;
using HealthChecksUI;

namespace Aspire.Hosting;

public static class HealthChecksUIExtensions
{
    /// <summary>
    /// Adds a HealthChecksUI container to the application model.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="name">The resource name.</param>
    /// <param name="port">The host port to expose the container on.</param>
    /// <param name="tag">The tag to use for the container image. Defaults to <c>"5.0.0"</c>.</param>
    /// <returns></returns>
    public static IResourceBuilder<HealthChecksUIResource> AddHealthChecksUI(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        string? tag = null)
    {
        builder.Services.TryAddLifecycleHook<HealthChecksUILifecycleHook>();

        var resource = new HealthChecksUIResource(name);

        return builder
            .AddResource(resource)
            .WithAnnotation(new ContainerImageAnnotation { Image = HealthChecksUIDefaults.ContainerImageName, Tag = tag ?? "5.0.0" })
            .WithEnvironment(HealthChecksUIResource.KnownEnvVars.UiPath, "/")
            .WithHttpEndpoint(hostPort: port, containerPort: HealthChecksUIDefaults.ContainerPort);
    }

    /// <summary>
    /// Adds a reference to a project that will be monitored by the HealthChecksUI container.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="project">The project.</param>
    /// <param name="probePath">The request path the project serves health check details from.</param>
    /// <param name="endpointName">The name of the endpoint the project serves health check details from. If it doesn't exist it will be added.</param>
    /// <param name="endpointPort">Port to use if creating a new endpoint for the health checks.</param>
    /// <returns></returns>
    public static IResourceBuilder<HealthChecksUIResource> WithReference(
        this IResourceBuilder<HealthChecksUIResource> builder,
        IResourceBuilder<ProjectResource> project,
        string probePath = HealthChecksUIDefaults.ProbePath,
        string endpointName = HealthChecksUIDefaults.EndpointName,
        int? endpointPort = null)
    {
        var healthCheck = new MonitoredProject(project, endpointName: endpointName, probePath: probePath)
        {
            EndpointPort = endpointPort
        };
        builder.Resource.MonitoredProjects.Add(healthCheck);

        return builder;
    }

    // TODO: Support referencing supported database containers and/or connection strings and configuring the HealthChecksUI container to use them
}

public static class HealthChecksUIDefaults
{
    public const string ContainerImageName = "xabarilcoding/healthchecksui";
    public const int ContainerPort = 80;
    public const string ProbePath = "/healthz";
    public const string EndpointName = "healthchecksui";
}
