using System.Diagnostics;
using Aspire.Hosting.Lifecycle;

namespace HealthChecksUI;

/// <summary>
/// A container-based resource for the HealthChecksUI container.
/// See https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks?tab=readme-ov-file#HealthCheckUI
/// </summary>
/// <param name="name">The resource name.</param>
public class HealthChecksUIResource(string name) : ContainerResource(name), IResourceWithServiceDiscovery
{
    /// <summary>
    /// The projects to be monitored by the HealthChecksUI container.
    /// </summary>
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

    /// <summary>
    /// The project to be monitored.
    /// </summary>
    public IResourceBuilder<ProjectResource> Project { get; } = project ?? throw new ArgumentNullException(nameof(project));

    /// <summary>
    /// The name of the endpoint the project serves health check details from. If it doesn't exist it will be added.
    /// </summary>
    public string EndpointName { get; } = endpointName ?? throw new ArgumentNullException(nameof(endpointName));

    /// <summary>
    /// The name of the project to be displayed in the HealthChecksUI dashboard. Defaults to the project resource's name.
    /// </summary>
    public string Name
    {
        get => _name ?? Project.Resource.Name;
        set { _name = value; }
    }

    /// <summary>
    /// The request path the project serves health check details for the HealthChecksUI dashboard from.
    /// </summary>
    public string ProbePath { get; set; } = probePath ?? throw new ArgumentNullException(nameof(probePath));
}

internal class HealthChecksUILifecycleHook(DistributedApplicationExecutionContext executionContext) : IDistributedApplicationLifecycleHook
{
    private const int DefaultAspNetCoreContainerPort = 8080;
    private const int DefaultHealthChecksPort = 8081;
    private const string HEALTHCHECKSUI_URLS = "HEALTHCHECKSUI_URLS";
    private const string ASPNETCORE_HTTP_PORTS = "ASPNETCORE_HTTP_PORTS";
    private const string ASPNETCORE_HTTPS_PORTS = "ASPNETCORE_HTTPS_PORTS";

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        // Configure each project referenced by a Health Checks UI resource
        var healthChecksUIResources = appModel.Resources.OfType<HealthChecksUIResource>();

        foreach (var healthChecksUIResource in healthChecksUIResources)
        {
            foreach (var monitoredProject in healthChecksUIResource.MonitoredProjects)
            {
                var project = monitoredProject.Project;

                // Add the health check endpoint if it doesn't exist
                var healthChecksEndpoint = project.GetEndpoint(monitoredProject.EndpointName);
                if (!healthChecksEndpoint.Exists)
                {
                    // WORKAROUND: https://github.com/dotnet/aspire/issues/3786
                    // Need to set a concrete target port in publish mode
                    int? targetPort = executionContext.IsPublishMode ? DefaultHealthChecksPort : null;
                    project.WithHttpEndpoint(targetPort: targetPort, name: monitoredProject.EndpointName);
                    Debug.Assert(healthChecksEndpoint.Exists, "The health check endpoint should exist after adding it.");
                }

                // Set environment variable to configure the URLs the health check endpoint is accessible from
                project.WithEnvironment(context =>
                {
                    var probePath = monitoredProject.ProbePath.TrimStart('/');
                    var healthChecksEndpointsExpression = ReferenceExpression.Create($"{healthChecksEndpoint}/{probePath}");

                    if (context.ExecutionContext.IsRunMode)
                    {
                        // Running during dev inner-loop
                        var containerHost = healthChecksUIResource.GetEndpoint("http").ContainerHost;
                        var fromContainerUriBuilder = new UriBuilder(healthChecksEndpoint.Url)
                        {
                            Host = containerHost,
                            Path = monitoredProject.ProbePath
                        };

                        healthChecksEndpointsExpression = ReferenceExpression.Create($"{healthChecksEndpointsExpression};{fromContainerUriBuilder.ToString()}");
                    }
                    else if (context.ExecutionContext.IsPublishMode)
                    {
                        // WORKAROUND: https://github.com/dotnet/aspire/issues/3749
                        if (!context.EnvironmentVariables.ContainsKey(ASPNETCORE_HTTP_PORTS)
                            && healthChecksEndpoint.Scheme is "http" && healthChecksEndpoint.TargetPort is int httpPort && httpPort != DefaultAspNetCoreContainerPort)
                        {
                            // The target port is different from the default port so set ASPNETCORE_HTTP_PORTS to listen on both ports
                            context.EnvironmentVariables.Add(
                                ASPNETCORE_HTTP_PORTS,
                                ReferenceExpression.Create($"{DefaultAspNetCoreContainerPort.ToString()};{healthChecksEndpoint.Property(EndpointProperty.TargetPort)}"));
                        }
                        if (!context.EnvironmentVariables.ContainsKey(ASPNETCORE_HTTPS_PORTS)
                            && healthChecksEndpoint.Scheme is "https" && healthChecksEndpoint.TargetPort.HasValue)
                        {
                            // The target port is for HTTPS so set ASPNETCORE_HTTPS_PORTS to listen on that port
                            context.EnvironmentVariables.Add(
                                ASPNETCORE_HTTPS_PORTS,
                                ReferenceExpression.Create($"{healthChecksEndpoint.Property(EndpointProperty.TargetPort)}"));
                        }
                    }

                    context.EnvironmentVariables.Add(HEALTHCHECKSUI_URLS, healthChecksEndpointsExpression);
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
            var monitoredProjects = healthChecksUIResource.MonitoredProjects;

            // Add environment variables to configure the HealthChecksUI container with the health checks endpoints of each referenced project
            // See example configuration at https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks?tab=readme-ov-file#sample-2-configuration-using-appsettingsjson
            for (var i = 0; i < monitoredProjects.Count; i++)
            {
                var monitoredProject = monitoredProjects[i];
                var healthChecksEndpoint = monitoredProject.Project.GetEndpoint(monitoredProject.EndpointName);

                // Set health check name
                var nameEnvVarName = HealthChecksUIResource.KnownEnvVars.GetHealthCheckNameKey(i);
                healthChecksUIResource.Annotations.Add(
                    new EnvironmentCallbackAnnotation(
                        nameEnvVarName,
                        () => monitoredProject.Name));

                // Set health check URL
                var probePath = monitoredProject.ProbePath.TrimStart('/');
                var urlEnvVarName = HealthChecksUIResource.KnownEnvVars.GetHealthCheckUriKey(i);

                healthChecksUIResource.Annotations.Add(
                    new EnvironmentCallbackAnnotation(
                        context => context[urlEnvVarName] = isPublishing
                            ? ReferenceExpression.Create($"{healthChecksEndpoint}/{probePath}")
                            : new HostUrl($"{healthChecksEndpoint.Url}/{probePath}")));
            }
        }
    }
}
