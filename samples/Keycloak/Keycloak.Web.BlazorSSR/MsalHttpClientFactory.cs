using Microsoft.Identity.Client;

namespace Keycloak.Web.BlazorSSR;

/// <summary>
/// Implementation of <see cref="IMsalHttpClientFactory"/> that creates a named <see cref="HttpClient"/> using <see cref="IHttpClientFactory"/>.<br />
/// The client name used is defined in <see cref="HttpClientName"/>.
/// </summary>
public class MsalHttpClientFactory(IHttpClientFactory httpClientFactory) : IMsalHttpClientFactory
{
    public const string HttpClientName = "MSAL";

    public HttpClient GetHttpClient() => httpClientFactory.CreateClient(HttpClientName);
}
