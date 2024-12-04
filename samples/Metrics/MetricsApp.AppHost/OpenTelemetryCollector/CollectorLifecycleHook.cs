using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace MetricsApp.AppHost.OpenTelemetryCollector;

internal sealed class CollectorLifecycleHook : IDistributedApplicationLifecycleHook
{
    private readonly ILogger<CollectorLifecycleHook> _logger;

    public CollectorLifecycleHook(ILogger<CollectorLifecycleHook> logger)
    {
        _logger = logger;
    }

    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var resources = appModel.GetProjectResources();
        var collectorResource = appModel.Resources.OfType<CollectorResource>().FirstOrDefault();

        if (collectorResource == null)
        {
            _logger.LogWarning("No collector resource found");
            return Task.CompletedTask;
        }

        var endpoint = collectorResource.GetEndpoint(CollectorResource.OtlpGrpcEndpointName);
        if (endpoint == null)
        {
            _logger.LogWarning("No endpoint for the collector");
            return Task.CompletedTask;
        }

        if (resources.Count() == 0)
        {
            _logger.LogInformation("No resources to add Environment Variables to");
        }

        foreach (var resourceItem in resources)
        {
            _logger.LogDebug($"Forwarding Telemetry for {resourceItem.Name} to the collector");
            if (resourceItem == null)
            {
                continue;
            }

            resourceItem.Annotations.Add(new EnvironmentCallbackAnnotation((EnvironmentCallbackContext context) =>
            {
                context.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = endpoint;
            }));
        }

        return Task.CompletedTask;
    }
}
