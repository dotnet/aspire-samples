using ConsoleApp;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

ConfigureOpenTelemetry(builder);

builder.Services.AddHostedService<NuGetDownloader>();

var host = builder.Build();
host.Run();

static IHostApplicationBuilder ConfigureOpenTelemetry(IHostApplicationBuilder builder)
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
    });

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(c => c.AddService("ConsoleApp"))
        .WithMetrics(metrics =>
        {
            metrics.AddHttpClientInstrumentation()
                   .AddRuntimeInstrumentation();
        })
        .WithTracing(tracing =>
        {
            tracing.AddHttpClientInstrumentation();
            tracing.AddSource(NuGetDownloader.ActivitySourceName);
        });

    // Use the OTLP exporter if the endpoint is configured.
    var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
    if (useOtlpExporter)
    {
        builder.Services.AddOpenTelemetry().UseOtlpExporter();
    }

    return builder;
}
