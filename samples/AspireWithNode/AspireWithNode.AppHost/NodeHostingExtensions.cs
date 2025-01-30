using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

internal static class NodeHostingExtensions
{
    /// <summary>
    /// Injects the ASP.NET Core HTTPS developer certificate into the resource via the specified environment variables when
    /// <paramref name="builder"/>.<see cref="IResourceBuilder{T}.ApplicationBuilder">ApplicationBuilder</see>.<see cref="IDistributedApplicationBuilder.ExecutionContext">ExecutionContext</see>.<see cref="DistributedApplicationExecutionContext.IsRunMode">IsRunMode</see><c> == true</c>.<br/>
    /// </summary>
    public static IResourceBuilder<NodeAppResource> RunWithHttpsDevCertificate(this IResourceBuilder<NodeAppResource> builder, CertificateFileFormat certificateFileFormat, string certFileEnv, string? certPasswordOrKeyEnv)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode && builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            DevCertHostingExtensions.RunWithHttpsDevCertificate(builder, certificateFileFormat, certFileEnv, certPasswordOrKeyEnv, (services, certFilePath, certPasswordOrKeyPath) =>
            {
                builder.WithHttpsEndpoint(env: "HTTPS_PORT");
                var httpsEndpoint = builder.GetEndpoint("https");
                
                // Configure Node to trust the ASP.NET Core HTTPS development certificate as a root CA.
                builder.WithEnvironment(async context =>
                {
                    var logger = services.GetRequiredService<ResourceLoggerService>().GetLogger(builder.Resource);
                    var (succeded, pemFilePath, _) = await DevCertHostingExtensions.TryExportDevCertificateAsync(CertificateFileFormat.Pem, builder.ApplicationBuilder, logger);
                    context.EnvironmentVariables["NODE_EXTRA_CA_CERTS"] = pemFilePath;
                    context.EnvironmentVariables["HTTPS_REDIRECT_PORT"] = ReferenceExpression.Create($"{httpsEndpoint.Property(EndpointProperty.Port)}");
                });

                return Task.CompletedTask;
            });
        }

        return builder;
    }
}
