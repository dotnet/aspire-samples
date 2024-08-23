using System.Diagnostics;
using System.IO.Hashing;
using System.Text;

namespace Aspire.Hosting;

public static class HostingExtensions
{
    /// <summary>
    /// Configures the parameter resource with a generated default value. The generated default value is stored in user secrets during local development.
    /// </summary>
    public static IResourceBuilder<ParameterResource> WithGeneratedDefault(this IResourceBuilder<ParameterResource> builder, GenerateParameterDefault generateParameterDefault)
    {
        var generatedParameter = ParameterResourceBuilderExtensions.CreateGeneratedParameter(builder.ApplicationBuilder, builder.Resource.Name, builder.Resource.Secret, generateParameterDefault);

        builder.Resource.Default = generatedParameter.Default;

        // Need to override the initial state as parameters attempt to bind from configuration first and move to an error state if it's not found.
        // This is adapted from the source of the internal static helper AddParameter(this IDistributedApplicationBuilder builder, ...) on Aspire.Hosting.ParameterResourceBuilderExtensions.
        builder.WithInitialState(new()
        {
            ResourceType = "Parameter",
            // Hide parameters by default
            State = KnownResourceStates.Hidden,
            Properties = [
                new("parameter.secret", builder.Resource.Secret.ToString()),
                new(CustomResourceKnownProperties.Source, $"Parameters:{builder.Resource.Name}")
            ]
        });

        return builder;
    }

    /// <summary>
    /// Injects the ASP.NET Core HTTPS developer certificate into the resource via the specified environment variables when
    /// <paramref name="builder"/>.<see cref="IResourceBuilder{T}.ApplicationBuilder">ApplicationBuilder</see>.
    /// <see cref="IDistributedApplicationBuilder.ExecutionContext">ExecutionContext</see>.<see cref="DistributedApplicationExecutionContext.IsRunMode">IsRunMode</see><c> == true</c>.<br/>
    /// If the resource is a <see cref="ContainerResource"/>, the certificate files will be bind mounted into the container.
    /// </summary>
    /// <remarks>
    /// This method <strong>does not</strong> configure an HTTPS endpoint on the resource. Use <see cref="ResourceBuilderExtensions.WithHttpsEndpoint{TResource}"/> to configure an HTTPS endpoint.
    /// </remarks>
    public static IResourceBuilder<TResource> RunWithHttpsDevCertificate<TResource>(this IResourceBuilder<TResource> builder, string certFileEnv, string certKeyFileEnv)
        where TResource : IResourceWithEnvironment
    {
        const string DEV_CERT_DIR = "/dev-certs";

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Export the ASP.NET Core HTTPS devlopment certificate & private key to PEM files, bind mount them into the container
            // and configure it to use them via the specified environment variables.
            var (certPath, _) = ExportDevCertificate(builder.ApplicationBuilder);
            var bindSource = Path.GetDirectoryName(certPath) ?? throw new UnreachableException();

            if (builder.Resource is ContainerResource containerResource)
            {
                builder.ApplicationBuilder.CreateResourceBuilder(containerResource)
                    .WithBindMount(bindSource, DEV_CERT_DIR, isReadOnly: true);
            }

            builder
                .WithEnvironment(certFileEnv, $"{DEV_CERT_DIR}/dev-cert.pem")
                .WithEnvironment(certKeyFileEnv, $"{DEV_CERT_DIR}/dev-cert.key");
        }

        return builder;
    }

    private static (string, string) ExportDevCertificate(IDistributedApplicationBuilder builder)
    {
        // Exports the ASP.NET Core HTTPS development certificate & private key to PEM files using 'dotnet dev-certs https' to a temporary
        // directory and returns the path.
        // TODO: Check if we're running on a platform that already has the cert and key exported to a file (e.g. macOS) and just use those intead.
        var appNameHashBytes = XxHash64.Hash(Encoding.Unicode.GetBytes(builder.Environment.ApplicationName).AsSpan());
        var appNameHash = BitConverter.ToString(appNameHashBytes).Replace("-", "").ToLowerInvariant();
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire.{appNameHash}");
        var certExportPath = Path.Combine(tempDir, "dev-cert.pem");
        var certKeyExportPath = Path.Combine(tempDir, "dev-cert.key");

        if (File.Exists(certExportPath) && File.Exists(certKeyExportPath))
        {
            // Certificate already exported, return the path.
            return (certExportPath, certKeyExportPath);
        }
        else if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }

        var exportProcess = Process.Start("dotnet", $"dev-certs https --export-path \"{certExportPath}\" --format Pem --no-password");

        var exited = exportProcess.WaitForExit(TimeSpan.FromSeconds(5));
        if (exited && File.Exists(certExportPath) && File.Exists(certKeyExportPath))
        {
            return (certExportPath, certKeyExportPath);
        }
        else if (exportProcess.HasExited && exportProcess.ExitCode != 0)
        {
            throw new InvalidOperationException($"HTTPS dev certificate export failed with exit code {exportProcess.ExitCode}");
        }
        else if (!exportProcess.HasExited)
        {
            exportProcess.Kill(true);
            throw new InvalidOperationException("HTTPS dev certificate export timed out");
        }

        throw new InvalidOperationException("HTTPS dev certificate export failed for an unknown reason");
    }
}
