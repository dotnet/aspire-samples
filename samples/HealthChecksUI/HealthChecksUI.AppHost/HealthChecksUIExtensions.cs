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
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<HealthChecksUIResource> AddHealthChecksUI(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null)
    {
        builder.Services.TryAddLifecycleHook<HealthChecksUILifecycleHook>();

        var resource = new HealthChecksUIResource(name);

        return builder
            .AddResource(resource)
            .WithImage(HealthChecksUIDefaults.ContainerImageName, HealthChecksUIDefaults.ContainerImageTag)
            .WithImageRegistry(HealthChecksUIDefaults.ContainerRegistry)
            .WithEnvironment(HealthChecksUIResource.KnownEnvVars.UiPath, "/")
            .WithHttpEndpoint(port: port, targetPort: HealthChecksUIDefaults.ContainerPort);
    }

    /// <summary>
    /// Adds a reference to a project that will be monitored by the HealthChecksUI container.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="project">The project.</param>
    /// <param name="endpointName">
    /// The name of the HTTP endpoint the <see cref="ProjectResource"/> serves health check details from.
    /// The endpoint will be added if it is not already defined on the <see cref="ProjectResource"/>.
    /// </param>
    /// <param name="probePath">The request path the project serves health check details from.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<HealthChecksUIResource> WithReference(
        this IResourceBuilder<HealthChecksUIResource> builder,
        IResourceBuilder<ProjectResource> project,
        string endpointName = HealthChecksUIDefaults.EndpointName,
        string probePath = HealthChecksUIDefaults.ProbePath)
    {
        var monitoredProject = new MonitoredProject(project, endpointName: endpointName, probePath: probePath);
        builder.Resource.MonitoredProjects.Add(monitoredProject);

        return builder;
    }

    // IDEA: Support referencing supported database containers and/or connection strings and configuring the HealthChecksUI container to use them
    //       https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/blob/master/doc/ui-docker.md#Storage-Providers-Configuration
}

/// <summary>
/// Default values used by <see cref="HealthChecksUIResource">.
/// </summary>
public static class HealthChecksUIDefaults
{
    /// <summary>
    /// The default container registry to pull the HealthChecksUI container image from.
    /// </summary>
    public const string ContainerRegistry = "docker.io";

    /// <summary>
    /// The default container image name to use for the HealthChecksUI container.
    /// </summary>
    public const string ContainerImageName = "xabarilcoding/healthchecksui";

    /// <summary>
    /// The default container image tag to use for the HealthChecksUI container.
    /// </summary>
    public const string ContainerImageTag = "5.0.0";

    /// <summary>
    /// The target port the HealthChecksUI container listens on.
    /// </summary>
    public const int ContainerPort = 80;

    /// <summary>
    /// The default request path projects serve health check details from.
    /// </summary>
    public const string ProbePath = "/healthz";

    /// <summary>
    /// The default name of the HTTP endpoint projects serve health check details from.
    /// </summary>
    public const string EndpointName = "healthchecks";
}
