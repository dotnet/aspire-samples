using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Keycloak.Web;

public class WeatherApiClient(HttpClient httpClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/api/weatherforecast", cancellationToken))
        {
            if (forecasts?.Count >= maxItems)
            {
                break;
            }
            if (forecast is not null)
            {
                forecasts ??= [];
                forecasts.Add(forecast);
            }
        }

        return forecasts?.ToArray() ?? [];
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public static class WeatherApiClientExtensions
{
    public static IServiceCollection AddWeatherApiClient(this IServiceCollection services, Uri weatherApiBaseAddress, string keycloakServiceName)
    {
        // Register HttpClient that MSAL will use to acquire tokens from Keycloak
        services.AddHttpClient(MsalHttpClientFactory.HttpClientName, (sp, client)
            => client.BaseAddress = new(sp.GetRequiredService<KeycloakUrls>().GetRealmUrl(keycloakServiceName)));

        services.AddSingleton<IMsalHttpClientFactory, MsalHttpClientFactory>();
        
        services.AddSingleton(sp =>
            {
                var oidcOptions = sp.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);
                // TODO: Need to investigate using Microsoft.Identity.Web instead of MSAL.NET directly.
                //       Additionally should consider using a distributed cache for the token cache, e.g. Redis, so tokens are cached
                //       across multiple app instances as per documented recommendations at
                //       https://learn.microsoft.com/entra/msal/dotnet/how-to/token-cache-serialization?tabs=msal#distributed-caches
                var app = ConfidentialClientApplicationBuilder.Create(oidcOptions.ClientId)
                    .WithClientSecret(oidcOptions.ClientSecret)
                    // NOTE: MSAL doesn't allow non-HTTPS authority URLs so this sample ensures Keycloak is configured to use HTTPS by
                    //       using the ASP.NET Core HTTPS development certificate.
                    .WithOidcAuthority(sp.GetRequiredService<KeycloakUrls>().GetRealmUrl(keycloakServiceName))
                    .WithInstanceDiscovery(false)
                    // Configure MSAL token caching as per
                    // https://learn.microsoft.com/entra/msal/dotnet/how-to/token-cache-serialization?tabs=msal#memory-cache-without-eviction/
                    // BUG: I don't think this is working as expected as access tokens are not being cached.
                    .WithLegacyCacheCompatibility(false)
                    .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                    // Configure MSAL to use IHttpClientFactory via our custom IMsalHttpClientFactory implementation
                    .WithHttpClientFactory(sp.GetRequiredService<IMsalHttpClientFactory>())
                    .Build();
                return app;
            }
        );

        services.AddHttpClient<WeatherApiClient>(client => client.BaseAddress = weatherApiBaseAddress)
            .AddHttpMessageHandler<AppAuthenticationMessageHandler>();

        services.AddTransient<AppAuthenticationMessageHandler>();

        return services;
    }

    /// <summary>
    /// Implementation of <see cref="IMsalHttpClientFactory"/> that creates a named <see cref="HttpClient"/> using <see cref="IHttpClientFactory"/>.<br />
    /// The client name used is defined in <see cref="HttpClientName"/>.
    /// </summary>
    private class MsalHttpClientFactory(IHttpClientFactory httpClientFactory) : IMsalHttpClientFactory
    {
        public const string HttpClientName = "MSAL";

        public HttpClient GetHttpClient() => httpClientFactory.CreateClient(HttpClientName);
    }

    /// <summary>
    /// A DelegatingHandler implementation that adds an authorization header with a bearer token for the application.
    /// </summary>
    private class AppAuthenticationMessageHandler(ILogger<AppAuthenticationMessageHandler> logger, IConfidentialClientApplication confidentialClientApplication)
        : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // MSAL requires that at least one scope be passed here but Keycloak is configured to return all the scopes we need by default
            // so we're just requesting the "roles" scope to satisfy MSAL.
            // REVIEW: Ideally we'd ask for a scope associated with the API we're calling but I haven't figured out how to setup Keycloak to do that yet.
            var authenticationResult = await confidentialClientApplication.AcquireTokenForClient(["roles"])
                .ExecuteAsync(cancellationToken);

            if (authenticationResult.AuthenticationResultMetadata.TokenSource != TokenSource.Cache)
            {
                // BUG: This is always being hit as the access token is never found in the cache for some reason.
                logger.LogInformation("No access token found in the cache. TokenSource: {tokensource}", authenticationResult.AuthenticationResultMetadata.TokenSource);
            }

            request.Headers.Authorization = new("Bearer", authenticationResult.AccessToken);

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
