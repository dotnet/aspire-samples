using Microsoft.Extensions.Hosting;

namespace MetricsApp.AppHost.OpenTelemetryCollector;

public static class OpenTelemetryCollectorResourceBuilderExtensions
{
    private const string DashboardOtlpUrlVariableName = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string DashboardOtlpApiKeyVariableName = "AppHost:OtlpApiKey";
    private const string DashboardOtlpUrlDefaultValue = "http://localhost:18889";

    public static IResourceBuilder<OpenTelemetryCollectorResource> AddOpenTelemetryCollector(this IDistributedApplicationBuilder builder, string name, string configFileLocation)
    {
        builder.AddOpenTelemetryCollectorInfrastructure();

        var url = builder.Configuration[DashboardOtlpUrlVariableName] ?? DashboardOtlpUrlDefaultValue;
        var isHttpsEnabled = url.StartsWith("https", StringComparison.OrdinalIgnoreCase);

        var dashboardOtlpEndpoint = new HostUrl(url);

        var resource = new OpenTelemetryCollectorResource(name);
        var resourceBuilder = builder.AddResource(resource)
            .WithImage("ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib", "latest")
            .WithEndpoint(targetPort: 4317, name: OpenTelemetryCollectorResource.OtlpGrpcEndpointName, scheme: isHttpsEnabled ? "https" : "http")
            .WithEndpoint(targetPort: 4318, name: OpenTelemetryCollectorResource.OtlpHttpEndpointName, scheme: isHttpsEnabled ? "https" : "http")
            .WithBindMount(configFileLocation, "/etc/otelcol-contrib/config.yaml")
            .WithEnvironment("ASPIRE_ENDPOINT", $"{dashboardOtlpEndpoint}")
            .WithEnvironment("ASPIRE_API_KEY", builder.Configuration[DashboardOtlpApiKeyVariableName])
            .WithEnvironment("ASPIRE_INSECURE", isHttpsEnabled ? "false" : "true");

        if (isHttpsEnabled && builder.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
        {
            DevCertHostingExtensions.RunWithHttpsDevCertificate(resourceBuilder, "HTTPS_CERT_FILE", "HTTPS_CERT_KEY_FILE", (certFilePath, certKeyPath) =>
            {
                // Set TLS details using YAML path via the command line. This allows the values to be added to the existing config file.
                // Setting the values in the config file doesn't work because adding the "tls" section always enables TLS, even if there is no cert provided.
                resourceBuilder.WithArgs(
                    @"--config=yaml:receivers::otlp::protocols::grpc::tls::cert_file: ""dev-certs/dev-cert.pem""",
                    @"--config=yaml:receivers::otlp::protocols::grpc::tls::key_file: ""dev-certs/dev-cert.key""",
                    @"--config=/etc/otelcol-contrib/config.yaml");
            });
        }

        return resourceBuilder;
    }
}
