namespace Aspire.Hosting;

internal static class NodeHostingExtensions
{
    /// <summary>
    /// Injects the ASP.NET Core HTTPS developer certificate into the resource via the specified environment variables when
    /// <paramref name="builder"/>.<see cref="IResourceBuilder{T}.ApplicationBuilder">ApplicationBuilder</see>.<see cref="IDistributedApplicationBuilder.ExecutionContext">ExecutionContext</see>.<see cref="DistributedApplicationExecutionContext.IsRunMode">IsRunMode</see><c> == true</c>.<br/>
    /// </summary>
    /// <remarks>
    /// This method <strong>does not</strong> configure an HTTPS endpoint on the resource. Use <see cref="ResourceBuilderExtensions.WithHttpsEndpoint{TResource}"/> to configure an HTTPS endpoint.
    /// </remarks>
    public static IResourceBuilder<NodeAppResource> RunWithHttpsDevCertificate(this IResourceBuilder<NodeAppResource> builder, string? certFileEnv = null, string? certKeyFileEnv = null)
    {
        certFileEnv ??= "HTTPS_CERT_FILE";
        certKeyFileEnv ??= "HTTPS_CERT_KEY_FILE";

        DevCertHostingExtensions.RunWithHttpsDevCertificate(builder, certFileEnv, certKeyFileEnv);

        builder.WithEnvironment(context =>
        {
            var certPath = context.EnvironmentVariables[certFileEnv];
            context.EnvironmentVariables["NODE_EXTRA_CA_CERTS"] = certPath;
        });

        return builder;
    }
}
