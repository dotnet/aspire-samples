using System.Diagnostics;
using System.Net.Http.Json;
using ConsoleApp.Model;
using OpenTelemetry.Trace;

namespace ConsoleApp;

public class PokemonDownloader(
    ILogger<PokemonDownloader> logger,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    public const string ActivitySourceName = "DownloadPokemon";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = s_activitySource.StartActivity("Downloading Pokemon", ActivityKind.Consumer);

        try
        {
            await DownloadPokemonAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private async Task DownloadPokemonAsync(CancellationToken cancellationToken)
    {
        var httpClient = new HttpClient();
        var nextPage = "https://pokeapi.co/api/v2/pokemon?limit=100";
        var currentDownloaded = 0;

        do
        {
            logger.LogInformation("Getting next page of results: {NextUrl}", nextPage);
            var list = await httpClient.GetFromJsonAsync<PokemonList>(nextPage, cancellationToken);
            if (list is null)
            {
                break;
            }

            foreach (var pokemon in list.Results)
            {
                logger.LogInformation("Pokemon: {Pokemon}", pokemon.Name);
                currentDownloaded++;
            }

            logger.LogInformation("Downloaded Pokemon: {Current} of {Total}", currentDownloaded, list.Count);
            nextPage = list.Next;
        } while (nextPage != null && !cancellationToken.IsCancellationRequested);
    }
}
