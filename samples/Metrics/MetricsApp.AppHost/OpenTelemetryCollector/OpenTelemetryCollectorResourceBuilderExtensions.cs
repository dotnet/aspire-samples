using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MetricsApp.AppHost.OpenTelemetryCollector;

public static class OpenTelemetryCollectorResourceBuilderExtensions
{
    private const string OtelExporterOtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string DashboardOtlpUrlVariableName = "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string DashboardOtlpApiKeyVariableName = "AppHost:OtlpApiKey";
    private const string DashboardOtlpUrlDefaultValue = "http://localhost:18889";
    private const string OTelCollectorImageName = "ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib";
    private const string OTelCollectorImageTag = "0.123.0";

    public static IResourceBuilder<OpenTelemetryCollectorResource> AddOpenTelemetryCollector(this IDistributedApplicationBuilder builder, string name, string configFileLocation)
    {
        var url = builder.Configuration[DashboardOtlpUrlVariableName] ?? DashboardOtlpUrlDefaultValue;
        var isHttpsEnabled = url.StartsWith("https", StringComparison.OrdinalIgnoreCase);

        var dashboardOtlpEndpoint = new HostUrl(url);

        var collectorResource = new OpenTelemetryCollectorResource(name);
        var resourceBuilder = builder.AddResource(collectorResource)
            .WithImage(OTelCollectorImageName, OTelCollectorImageTag)
            .WithEndpoint(targetPort: 4317, name: OpenTelemetryCollectorResource.OtlpGrpcEndpointName, scheme: "http")
            .WithEndpoint(targetPort: 4318, name: OpenTelemetryCollectorResource.OtlpHttpEndpointName, scheme: "http")
            .WithUrlForEndpoint(OpenTelemetryCollectorResource.OtlpGrpcEndpointName, u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly)
            .WithUrlForEndpoint(OpenTelemetryCollectorResource.OtlpHttpEndpointName, u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly)
            .WithBindMount(configFileLocation, "/etc/otelcol-contrib/config.yaml")
            .WithEnvironment("ASPIRE_ENDPOINT", $"{dashboardOtlpEndpoint}")
            .WithEnvironment("ASPIRE_API_KEY", builder.Configuration[DashboardOtlpApiKeyVariableName])
            .WithEnvironment("ASPIRE_INSECURE", isHttpsEnabled ? "false" : "true");

        builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) =>
        {
            var logger = e.Services.GetRequiredService<ILogger<OpenTelemetryCollectorResource>>();
            var endpoint = collectorResource.GetEndpoint(OpenTelemetryCollectorResource.OtlpGrpcEndpointName);

            if (!endpoint.Exists)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning($"No {OpenTelemetryCollectorResource.OtlpGrpcEndpointName} endpoint for the collector.");
                }
                return Task.CompletedTask;
            }

            // Update all resources to forward telemetry to the collector.
            var appModel = e.Services.GetRequiredService<DistributedApplicationModel>();
            foreach (var resource in appModel.Resources)
            {
                resource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
                {
                    if (context.EnvironmentVariables.ContainsKey(OtelExporterOtlpEndpoint))
                    {
                        if (logger.IsEnabled(LogLevel.Debug))
                        {
                            logger.LogDebug("Forwarding telemetry for {ResourceName} to the collector.", resource.Name);
                        }

                        context.EnvironmentVariables[OtelExporterOtlpEndpoint] = endpoint;
                    }
                }));
            }

            return Task.CompletedTask;
        });

        if (isHttpsEnabled && builder.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
        {
            resourceBuilder.WithArgs(@"--config=/etc/otelcol-contrib/config.yaml");
        }

        return resourceBuilder;
    }
}
