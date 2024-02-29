using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static WebApplicationBuilder ConfigureKestrelDefaults(this WebApplicationBuilder builder)
    {
        // HACK: Ideally this could be done from the AppHost but the application model doesn't expose the required information yet
        var internalHealthChecksPort = builder.Configuration["INTERNAL_HEALTHCHECKS_LISTEN_PORT"];

        if (internalHealthChecksPort is not null)
        {
            HashSet<string> serverUrls = [..builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey)?.Split(';')];

            var internalHealthChecksHost = builder.Configuration["INTERNAL_HEALTHCHECKS_LISTEN_HOST"] ?? "localhost";
            var internalHealthChecksScheme = builder.Configuration["INTERNAL_HEALTHCHECKS_LISTEN_SCHEME"] ?? "http";
            var healthChecksUrl = $"{internalHealthChecksScheme}://{internalHealthChecksHost}:{internalHealthChecksPort}";
            serverUrls.Add(healthChecksUrl);

            // Configure Kestrel to listen on separate port for internal health checks
            builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, string.Join(';', serverUrls));
        }

        return builder;
    }

    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.UseServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddProcessInstrumentation()
                       .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    // We want to view all traces in development
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.AddAspNetCoreInstrumentation()
                       .AddGrpcClientInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
        }

        // Uncomment the following lines to enable the Prometheus exporter (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        // builder.Services.AddOpenTelemetry()
        //    .WithMetrics(metrics => metrics.AddPrometheusExporter());

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        // builder.Services.AddOpenTelemetry()
        //    .UseAzureMonitor();

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Uncomment the following line to enable the Prometheus endpoint (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        // app.MapPrometheusScrapingEndpoint();

        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks("/health");

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        // Health checks for the HealthChecks UI
        var internalHealthChecksPort = app.Configuration["INTERNAL_HEALTHCHECKS_PORT"];
        if (!string.IsNullOrEmpty(internalHealthChecksPort))
        {
            var internalHealthChecksPath = app.Configuration["INTERNAL_HEALTHCHECKS_PATH"] ?? "/healthz";
            var hostMask = $"*:{internalHealthChecksPort}";
            app.MapHealthChecks(internalHealthChecksPath, new() { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse })
                .RequireHost(hostMask);
        }

        return app;
    }
}
