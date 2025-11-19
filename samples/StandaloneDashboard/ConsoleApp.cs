#:package Microsoft.Extensions.Hosting@10.0.0
#:package OpenTelemetry.Exporter.OpenTelemetryProtocol@1.14.0
#:package OpenTelemetry.Extensions.Hosting@1.14.0
#:package OpenTelemetry.Instrumentation.Http@1.14.0
#:package OpenTelemetry.Instrumentation.Runtime@1.14.0
#:property PublishAot=false

using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

class NuGetDownloader(
    ILogger<NuGetDownloader> logger,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    public const string ActivitySourceName = "DownloadNuGetTopPackages";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = s_activitySource.StartActivity("Downloading NuGet top package details", ActivityKind.Consumer);

        try
        {
            await DownloadTopPackagesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private async Task DownloadTopPackagesAsync(CancellationToken cancellationToken, int rowCount = 10)
    {
        var httpClient = new HttpClient();
        var searchServiceUri = new UriBuilder(await GetSearchServiceUri())
        {
            Query = $"skip=0&take={Uri.EscapeDataString(rowCount.ToString(CultureInfo.InvariantCulture))}"
        }.Uri;

        logger.InformationLevel?.Log("Getting top {PackageCount} packages: {SearchUrl}", rowCount, searchServiceUri);
        var searchResults = await httpClient.GetFromJsonAsync<SearchResults>(searchServiceUri, cancellationToken)
            ?? throw new InvalidOperationException("Unexpected null search results.");

        foreach (var result in searchResults.Data)
        {
            logger.InformationLevel?.Log("Package: {Package} - {Description} ({TotalDownloads} downloads)", result.Id, result.Description, result.TotalDownloads);
        }
    }

    private static async Task<Uri> GetSearchServiceUri(Uri? serviceIndexAddress = null)
    {
        serviceIndexAddress ??= new Uri("https://api.nuget.org/v3/index.json");

        var httpClient = new HttpClient();
        var serviceIndex = await httpClient.GetFromJsonAsync<ServiceIndex>(serviceIndexAddress)
            ?? throw new ArgumentException("Service index did not return a valid result.", nameof(serviceIndexAddress));

        var searchService = serviceIndex.Resources.FirstOrDefault(r => r.Type == "SearchQueryService/3.5.0");

        return searchService?.IdUri ?? throw new ArgumentException("Search service not found in service index.", nameof(serviceIndexAddress));
    }

    private class ServiceIndex
    {
        public required string Version { get; set; }

        public List<Resource> Resources { get; set; } = [];
    }

    private class Resource
    {
        [JsonPropertyName("@id")]
        public Uri? IdUri { get; set; }
        [JsonPropertyName("@type")]
        public required string Type { get; set; }
        public string? Comment { get; set; }
    }

    private class SearchResults
    {
        public int TotalHits { get; set; }
        public SearchResult[] Data { get; set; } = [];
    }

    private class SearchResult
    {
        [JsonPropertyName("@id")]
        public required Uri IdUri { get; set; }
        public required string Id { get; set; }
        public required string Version { get; set; }
        public string? Description { get; set; }
        public string[]? Authors { get; set; }
        public long TotalDownloads { get; set; }
    }
}

static class LoggerExtensions
{
    extension(ILogger logger)
    {
        public LevelLogger? InformationLevel => logger.IsEnabled(LogLevel.Information) ? new(logger, LogLevel.Information) : null;
        public LevelLogger? WarngingLevel => logger.IsEnabled(LogLevel.Warning) ? new(logger, LogLevel.Warning) : null;
        public LevelLogger? ErrorLevel => logger.IsEnabled(LogLevel.Error) ? new(logger, LogLevel.Error) : null;
    }

    public struct LevelLogger(ILogger logger, LogLevel logLevel)
    {
        public readonly void Log(string message, params object?[] args) => logger.Log(logLevel, message, args);
    }
}