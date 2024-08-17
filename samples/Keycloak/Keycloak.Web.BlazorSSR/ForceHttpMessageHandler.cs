namespace Keycloak.Web.BlazorSSR;

public class ForceHttpMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.Scheme == "https")
        {
            var uriBuilder = new UriBuilder(request.RequestUri)
            {
                Scheme = "http"
            };
            if (request.RequestUri.Port == 443)
            {
                uriBuilder.Port = 80;
            }
            request.RequestUri = uriBuilder.Uri;
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
