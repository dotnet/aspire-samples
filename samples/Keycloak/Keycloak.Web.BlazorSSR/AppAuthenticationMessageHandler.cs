using Microsoft.Identity.Client;

namespace Keycloak.Web.BlazorSSR;

/// <summary>
/// A DelegatingHandler implementation that add an authorization header with a token for the application.
/// </summary>
public class AppAuthenticationMessageHandler(IConfidentialClientApplication confidentialClientApplication) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await confidentialClientApplication.AcquireTokenForClient(["api-callers"])
            .ExecuteAsync(cancellationToken);

        request.Headers.Authorization = new("Bearer", accessToken.AccessToken);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
