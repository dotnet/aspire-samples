using Aspire.Hosting.Lifecycle;

namespace MetricsApp.AppHost.ContainerProxy;

public static class ContainerProxyResourceExtensions
{
    /// <summary>
    /// Adds nginx container with name of <see cref="projectResourceBuilder"/> resource, which will proxy requests from other <see cref="ContainerResource"/>s to <see cref="ProjectResource"/> identified by <see cref="projectResourceBuilder"/>.
    /// </summary>
    /// <param name="projectResourceBuilder"></param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> WithContainerProxy(this IResourceBuilder<ProjectResource> projectResourceBuilder)
    {
        projectResourceBuilder.ApplicationBuilder.Services.TryAddLifecycleHook<ContainerProxyResourceLifecycleHook>();

        projectResourceBuilder.ApplicationBuilder.AddContainer(projectResourceBuilder.Resource.Name + "-proxy", "nginx")
            .WithAnnotation(new ContainerProxyAnnotation(projectResourceBuilder.Resource))
            .WithContainerRuntimeArgs(context =>
            {
                context.Args.Add("--network-alias");
                context.Args.Add(projectResourceBuilder.Resource.Name);

                context.Args.Add("--hostname");
                context.Args.Add(projectResourceBuilder.Resource.Name);
            })
            .WithHttpEndpoint(targetPort: 80, name: "http")
            .ExcludeFromManifest();

        return projectResourceBuilder;
    }
}
