using System;
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
    /// <param name="endpointName">The name of the endpoint the project serves health check details from.</param>
    /// <param name="probePath">The request path the project serves health check details from.</param>
    /// <returns></returns>
    public static IResourceBuilder<HealthChecksUIResource> WithReference(
        this IResourceBuilder<HealthChecksUIResource> builder,
        IResourceBuilder<ProjectResource> project,
        string endpointName = "http",
        string probePath = HealthChecksUIDefaults.ProbePath)
    {
        var monitoredProject = new MonitoredProject(project, endpointName: endpointName, probePath: probePath);
        builder.Resource.MonitoredProjects.Add(monitoredProject);

        return builder;
    }

    /// <summary>
    /// Indicates that a resource should be available by its endpoints externally.
    /// </summary>
    /// <remarks>
    /// This method will add an external endpoint for each existing non-external endpoint on the resource.<br />
    /// The external endpoint will have the same protocol, transport, and URI scheme as the internal endpoint. It will be named for the non-external
    /// endpoint with "-external" appended as a suffix.
    /// <para>
    /// To model a specific endpoint as available externally, use <see cref="WithEndpoint{T}(IResourceBuilder{T}, string, Action{EndpointAnnotation}, bool)"/>
    /// and set <see cref="EndpointAnnotation.IsExternal"/> to <see langword="true"/>, e.g.:
    /// <code>
    /// builder.WithEndpoint("http", endpoint => endpoint.IsExternal = true);
    /// </code>
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> AsExternal<T>(this IResourceBuilder<T> builder) where T : IResourceWithEndpoints
    {
        if (builder.Resource.TryGetEndpoints(out var endpoints))
        {
            var internalEndpoints = endpoints.Where(e => !e.IsExternal).ToArray();
            foreach (var endpoint in internalEndpoints)
            {
                var externalName = $"{endpoint.Name}-external";
                var existingExternalEndpoint = endpoints.FirstOrDefault(ea => string.Equals(ea.Name, externalName, StringComparison.OrdinalIgnoreCase));
                if (existingExternalEndpoint is null)
                {
                    builder.WithEndpoint(externalName, e =>
                    {
                        e.Protocol = endpoint.Protocol;
                        e.Transport = endpoint.Transport;
                        e.UriScheme = endpoint.UriScheme;
                        e.IsProxied = e.IsProxied;
                        e.IsExternal = true;
                        // We don't set the port here as it will be assigned by the orchestrator/deployer
                        // We don't set EnvironmentVariable here as we don't want to inject the external endpoint into the app as we'll rely on DCP
                    });
                }
            }
        }

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
