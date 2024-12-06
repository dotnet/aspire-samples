using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace MetricsApp.AppHost.OpenTelemetryCollector;

internal sealed class OpenTelemetryCollectorLifecycleHook : IDistributedApplicationLifecycleHook
{
    private readonly ILogger<OpenTelemetryCollectorLifecycleHook> _logger;

    public OpenTelemetryCollectorLifecycleHook(ILogger<OpenTelemetryCollectorLifecycleHook> logger)
    {
        _logger = logger;
    }

    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var collectorResource = appModel.Resources.OfType<OpenTelemetryCollectorResource>().FirstOrDefault();
        if (collectorResource == null)
        {
            _logger.LogWarning($"No {nameof(OpenTelemetryCollectorResource)} resource found.");
            return Task.CompletedTask;
        }

        var endpoint = collectorResource.GetEndpoint(OpenTelemetryCollectorResource.OtlpGrpcEndpointName);
        if (!endpoint.Exists)
        {
            _logger.LogWarning($"No {OpenTelemetryCollectorResource.OtlpGrpcEndpointName} endpoint for the collector.");
            return Task.CompletedTask;
        }

        foreach (var resource in appModel.GetProjectResources())
        {
            _logger.LogDebug("Forwarding telemetry for {ResourceName} to the collector.", resource.Name);

            resource.Annotations.Add(new EnvironmentCallbackAnnotation((EnvironmentCallbackContext context) =>
            {
                context.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = endpoint;
            }));
        }

        return Task.CompletedTask;
    }
}
