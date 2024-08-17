using Microsoft.Identity.Client;

namespace Keycloak.Web.BlazorSSR;

public class MsalHttpClientFactory(IHttpClientFactory httpClientFactory) : IMsalHttpClientFactory
{
    public HttpClient GetHttpClient() => httpClientFactory.CreateClient("Msal");
}
