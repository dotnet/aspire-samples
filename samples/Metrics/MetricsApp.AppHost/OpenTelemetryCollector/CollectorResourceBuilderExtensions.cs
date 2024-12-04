using Microsoft.Extensions.Configuration;

namespace MetricsApp.AppHost.OpenTelemetryCollector;

public static class CollectorResourceBuilderExtensions
{
    private const string DashboardOtlpUrlVariableName = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string DashboardOtlpApiKeyVariableName = "AppHost:OtlpApiKey";
    private const string DashboardOtlpUrlDefaultValue = "http://localhost:18889";

    public static IResourceBuilder<CollectorResource> AddCollector(this IDistributedApplicationBuilder builder, string name, string configFileLocation)
    {
        builder.AddCollectorInfrastructure();

        var url = builder.Configuration[DashboardOtlpUrlVariableName] ?? DashboardOtlpUrlDefaultValue;

        var dashboardOtlpEndpoint = new HostUrl(url);
        var dashboardInsecure = url.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "false" : "true";

        var resource = new CollectorResource(name);
        return builder.AddResource(resource)
            .WithImage("ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib", "latest")
            .WithEndpoint(port: 4317, targetPort: 4317, name: CollectorResource.OtlpGrpcEndpointName, scheme: "http")
            .WithEndpoint(port: 4318, targetPort: 4318, name: CollectorResource.OtlpHttpEndpointName, scheme: "http")
            .WithBindMount(configFileLocation, "/etc/otelcol-contrib/config.yaml")
            .WithEnvironment("ASPIRE_ENDPOINT", $"{dashboardOtlpEndpoint}")
            .WithEnvironment("ASPIRE_API_KEY", builder.Configuration[DashboardOtlpApiKeyVariableName])
            .WithEnvironment("ASPIRE_INSECURE", dashboardInsecure);
    }
}
