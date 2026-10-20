namespace Aspire.Hosting;

public static class KeycloakExtensions
{
    /// <summary>
    /// Configures the Keycloak container with the sample realm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note that the realm import will be skipped by Keycloak if it already exists (as of Keycloak 25).
    /// </para>
    /// <para>
    /// If the Keycloak resource is configured to use a data volume, this import will only happen once.
    /// Delete the data volume to force the realm import to happen again.
    /// </para>
    /// <para>
    /// To update the sample realm file it will need to be exported again using <c>docker exec</c>, and manually updated so that hard-coded values
    /// are replaced with environment variable expressions that match those specified in this method.
    /// </para>
    /// <para>
    /// See <see href="https://www.keycloak.org/server/importExport#_exporting_a_specific_realm">
    /// https://www.keycloak.org/server/importExport#_exporting_a_specific_realm</see> for details on exporting realms.<br/>
    /// See <see href="https://www.keycloak.org/server/importExport#_using_environment_variables_within_the_realm_configuration_files">
    /// https://www.keycloak.org/server/importExport#_using_environment_variables_within_the_realm_configuration_files</see>
    /// for details on using environment variables in realm import files.
    /// </para>
    /// </remarks>
    /// <param name="builder">The builder.</param>
    /// <param name="realmName">The parameter for the sample realm name.</param>
    /// <param name="displayName">The parameter for the sample realm display name.</param>
    /// <param name="clientDetails">The details of the clients the sample realm uses.</param>
    public static IResourceBuilder<KeycloakResource> WithSampleRealmImport(this IResourceBuilder<KeycloakResource> builder,
        IResourceBuilder<ParameterResource> realmName,
        IResourceBuilder<ParameterResource> displayName,
        IEnumerable<KeycloakClientDetails> clientDetails)
    {
        builder
            .WithRealmImport("realms")
            .WithEnvironment("REALM_NAME", realmName)
            .WithEnvironment("REALM_DISPLAY_NAME", displayName)
            // Ensure HSTS is not enabled in run mode to avoid browser caching issues when developing.
            // Workaround for https://github.com/keycloak/keycloak/issues/32366
            .WithEnvironment("REALM_HSTS", builder.ApplicationBuilder.ExecutionContext.IsRunMode ? "" : "max-age=31536000; includeSubDomains");

        foreach (var client in clientDetails)
        {
            builder
                .WithEnvironment($"CLIENT_{client.EnvironmentVariable}_ID", client.ClientId)
                .WithEnvironment($"CLIENT_{client.EnvironmentVariable}_NAME", client.ClientName)
                .WithEnvironment($"CLIENT_{client.EnvironmentVariable}_SECRET", client.ClientSecret)
                .WithEnvironment(context =>
                {
                    // Inject the URLs of our apps into the Keycloak instance so they're picked up by the realms import file.
                    var httpsEndpoint = client.ClientResource.GetEndpoint("https");

                    // Ensure the correct URLs are used depending on the context, e.g. container reference vs. external HTTP endpoint, publish mode vs. run mode.
                    context.EnvironmentVariables[$"CLIENT_{client.EnvironmentVariable}_URL"] = context.ExecutionContext.IsPublishMode ? httpsEndpoint : httpsEndpoint.Url;
                    context.EnvironmentVariables[$"CLIENT_{client.EnvironmentVariable}_URL_CONTAINERHOST"] = httpsEndpoint;
                });
        }

        return builder;
    }
}

/// <summary>
/// Details for a Keycloak client used in the sample realm.
/// </summary>
/// <param name="EnvironmentVariable">
/// The environment variable name that the client details are injected into.<br/>
/// The realm import file uses environment variables named based on this value to reference the injected details,<br/>
/// e.g. <c>CLIENT_${<paramref name="EnvironmentVariable"/>}_ID</c>, <c>CLIENT_${<paramref name="EnvironmentVariable"/>}_SECRET</c>.
/// </param>
public record KeycloakClientDetails(
    string EnvironmentVariable,
    IResourceBuilder<ParameterResource> ClientId,
    IResourceBuilder<ParameterResource> ClientName,
    IResourceBuilder<ParameterResource> ClientSecret,
    IResourceBuilder<IResourceWithEndpoints> ClientResource);
