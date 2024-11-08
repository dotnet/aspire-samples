using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using OpenTelemetry.Trace;

namespace ConsoleApp;

public class NuGetDownloader(
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
            activity?.RecordException(ex);
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

        logger.LogInformation("Getting top {PackageCount} packages: {SearchUrl}", rowCount, searchServiceUri);
        var searchResults = await httpClient.GetFromJsonAsync<SearchResults>(searchServiceUri, cancellationToken)
            ?? throw new InvalidOperationException("");

        foreach (var result in searchResults.Data)
        {
            logger.LogInformation("Package: {Package} - {Description} ({TotalDownloads} downloads)", result.Id, result.Description, result.TotalDownloads);
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
