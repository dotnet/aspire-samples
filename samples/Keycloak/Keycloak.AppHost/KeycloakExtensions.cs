namespace Aspire.Hosting;

public static class KeycloakExtensions
{
    /// <summary>
    /// Configures the Keycloak container to use HTTPS with the ASP.NET Core HTTPS developer certificate.
    /// </summary>
    public static IResourceBuilder<KeycloakResource> WithHttpsDevCertificate(this IResourceBuilder<KeycloakResource> builder, int targetPort = 8443)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Mount the ASP.NET Core HTTPS devlopment certificate in the Keycloak container and configure Keycloak to it
            // via the KC_HTTPS_CERTIFICATE_FILE and KC_HTTPS_CERTIFICATE_KEY_FILE environment variables.
            builder
                .WithHttpsDevCertificate("KC_HTTPS_CERTIFICATE_FILE", "KC_HTTPS_CERTIFICATE_KEY_FILE")
                .WithHttpsEndpoint(env: "KC_HTTPS_PORT", targetPort: targetPort)
                .WithEnvironment("KC_HOSTNAME", "localhost")
                // Without disabling HTTP/2 you can hit HTTP 431 Header too large errors in Keycloak.
                // Related issues:
                // https://github.com/keycloak/keycloak/discussions/10236
                // https://github.com/keycloak/keycloak/issues/13933
                // https://github.com/quarkusio/quarkus/issues/33692
                .WithEnvironment("QUARKUS_HTTP_HTTP2", "false");

            // Remove the HTTP endpoint as Keycloak is redirecting to HTTPS on the wrong port.
            var httpEndpoint = builder.Resource.Annotations.OfType<EndpointAnnotation>().FirstOrDefault(a => a.Name == "http");
            if (httpEndpoint is not null)
            {
                builder.Resource.Annotations.Remove(httpEndpoint);
            }
        }

        return builder;
    }
}
