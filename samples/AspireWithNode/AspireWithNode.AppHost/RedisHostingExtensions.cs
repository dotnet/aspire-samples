using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

internal static class RedisHostingExtensions
{
    /// <summary>
    /// Configures the Redis resource to use the ASP.NET Core HTTPS developer certificate when
    /// <paramref name="builder"/>.<see cref="IResourceBuilder{T}.ApplicationBuilder">ApplicationBuilder</see>.<see cref="IDistributedApplicationBuilder.ExecutionContext">ExecutionContext</see>.<see cref="DistributedApplicationExecutionContext.IsRunMode">IsRunMode</see><c> == true</c>.<br/>
    /// </summary>
    public static IResourceBuilder<RedisResource> RunWithHttpsDevCertificate(this IResourceBuilder<RedisResource> builder)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode && builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            DevCertHostingExtensions.RunWithHttpsDevCertificate(builder, CertificateFileFormat.Pem, "TLS_CERT_FILE", "TLS_KEY_FILE", (services, certFilePath, certPasswordOrKeyPath) =>
            {
                // This callback is invoked during the BeforeStartEvent phase if the certificate is successfully exported.
                //builder.WithConnectionStringRedirection(new RedisTlsConnectionString(builder.Resource));

                // Configure Redis to use the ASP.NET Core HTTPS development certificate.
                //builder.WithArgs(context =>
                //{
                //    context.Args.Add("--port");
                //    context.Args.Add("0");
                //    context.Args.Add("--tls-port");
                //    context.Args.Add(builder.Resource.PrimaryEndpoint.TargetPort.ToString()!);
                //    context.Args.Add("--tls-cert-file");
                //    context.Args.Add($"{DevCertHostingExtensions.DEV_CERT_BIND_MOUNT_DEST_DIR}/{DevCertHostingExtensions.DEV_CERT_FILE_NAME_PEM}");
                //    context.Args.Add("--tls-key-file");
                //    context.Args.Add($"{DevCertHostingExtensions.DEV_CERT_BIND_MOUNT_DEST_DIR}/{DevCertHostingExtensions.DEV_CERT_FILE_NAME_KEY}");
                //    context.Args.Add("--tls-ca-cert-file");
                //    context.Args.Add($"{DevCertHostingExtensions.DEV_CERT_BIND_MOUNT_DEST_DIR}/{DevCertHostingExtensions.DEV_CERT_FILE_NAME_PEM}");
                //    context.Args.Add("--tls-auth-clients");
                //    context.Args.Add("no");
                //});

                return Task.CompletedTask;
            });
        }

        return builder;
    }

    private class RedisTlsConnectionString(RedisResource resource) : IResourceWithConnectionString
    {
        public string Name { get; } = $"{resource.Name}-tls";

        public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create(
            $"{resource.PrimaryEndpoint.Property(EndpointProperty.Host)}:{resource.PrimaryEndpoint.Property(EndpointProperty.Port)},Ssl=true");

        public ResourceAnnotationCollection Annotations { get; } = [];
    }
}
