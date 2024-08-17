using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ServiceDiscovery;

namespace Keycloak;

/// <summary>
/// A service that provides URLs for Keycloak. Register as a singleton.
/// </summary>
public class KeycloakUrls(IConfiguration configuration, ServiceEndpointResolver serviceEndpointResolver)
{
    private readonly string _realmName = configuration.GetRequiredValue("idpRealmName");
    private readonly ServiceEndpointResolver _serviceEndpointResolver = serviceEndpointResolver;

    /// <summary>
    /// Gets the URL for the Keycloak Account Console that allows end users to manage their own profile information.
    /// </summary>
    /// <remarks>
    /// See https://www.keycloak.org/docs/25.0.2/server_admin/#_account-service for more information on the Keycloak Account Console.
    /// </remarks>
    public async Task<string> GetAccountConsoleUrlAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return await GetUrlAsync(serviceName, "account", false, cancellationToken);
    }

    /// <summary>
    /// Gets the URL for the Keycloak server with the given service name.
    /// </summary>
    public async Task<string> GetUrlAsync(string serviceName, string? path = null, bool forServiceDiscovery = false, CancellationToken cancellationToken = default)
    {
        // $"https+http://{idpServiceName}/realms/{idpRealmName}";
        var serviceLookupName = "http://" + serviceName;
        var serviceAddresses = (await _serviceEndpointResolver.GetEndpointsAsync(serviceLookupName, cancellationToken))
            .Endpoints
            .Select(e => e.EndPoint.ToString())
            .ToList();

        var firstHttpsEndpointUrl = serviceAddresses.FirstOrDefault(e => e?.StartsWith("https://") == true);
        var endpointUrl = firstHttpsEndpointUrl ?? serviceAddresses.FirstOrDefault(e => e?.StartsWith("http://") == true);

        if (endpointUrl is null)
        {
            throw new InvalidOperationException($"No HTTP endpoints found for service '{serviceName}'.");
        }

        var uriBuilder = new UriBuilder(endpointUrl)
        {
            Path = $"/realms/{_realmName}"
        };

        if (forServiceDiscovery)
        {
            uriBuilder.Scheme = "https+http";
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            uriBuilder.Path += $"/{path}";
        }

        return uriBuilder.Uri.ToString();
    }
}
