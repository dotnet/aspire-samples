using Microsoft.Identity.Client;

namespace Keycloak.Web.BlazorSSR;

/// <summary>
/// A DelegatingHandler implementation that adds an authorization header with a bearer token for the application.
/// </summary>
public class AppAuthenticationMessageHandler(ILogger<AppAuthenticationMessageHandler> logger, IConfidentialClientApplication confidentialClientApplication) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // MSAL requires that at least one scope be passed here but Keycloak is configured to return all the scopes we need by default
        // so we're just requesting the "roles" scope to satisfy MSAL.
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
