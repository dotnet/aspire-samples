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
