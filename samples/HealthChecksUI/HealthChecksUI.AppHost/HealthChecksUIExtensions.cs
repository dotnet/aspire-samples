using Aspire.Hosting.Lifecycle;
using HealthChecksUI;

namespace Aspire.Hosting;

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
            .WithAnnotation(new ContainerImageAnnotation { Image = HealthChecksUIDefaults.ContainerImageName, Tag = tag ?? "5.0.0" })
            .WithEnvironment(HealthChecksUIContainerResource.EnvVars.UiPath, "/")
            .WithHttpEndpoint(hostPort: port, containerPort: HealthChecksUIDefaults.ContainerPort);
    }

    public static IResourceBuilder<HealthChecksUIContainerResource> WithReference(
        this IResourceBuilder<HealthChecksUIContainerResource> builder,
        IResourceBuilder<ProjectResource> project,
        string probePath = HealthChecksUIDefaults.ProbePath,
        string endpointName = HealthChecksUIDefaults.EndpointName,
        int? hostPort = null)
    {
        var healthCheck = new HealthCheckProject(project, endpointName: endpointName, probePath: probePath)
        {
            Port = hostPort
        };
        builder.Resource.HealthChecks.Add(healthCheck);

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
