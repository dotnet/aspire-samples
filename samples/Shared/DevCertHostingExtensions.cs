using System.Diagnostics;

namespace Aspire.Hosting;

public static class DevCertHostingExtensions
{
    /// <summary>
    /// Injects the ASP.NET Core HTTPS developer certificate into the resource via the specified environment variables when
    /// <paramref name="builder"/>.<see cref="IResourceBuilder{T}.ApplicationBuilder">ApplicationBuilder</see>.<see cref="IDistributedApplicationBuilder.ExecutionContext">ExecutionContext</see>.<see cref="DistributedApplicationExecutionContext.IsRunMode">IsRunMode</see><c> == true</c>.<br/>
    /// If the resource is a <see cref="ContainerResource"/>, the certificate files will be bind mounted into the container.
    /// </summary>
    /// <remarks>
    /// This method <strong>does not</strong> configure an HTTPS endpoint on the resource. Use <see cref="ResourceBuilderExtensions.WithHttpsEndpoint{TResource}"/> to configure an HTTPS endpoint.
    /// </remarks>
    public static IResourceBuilder<TResource> RunWithHttpsDevCertificate<TResource>(this IResourceBuilder<TResource> builder, string certFileEnv, string certKeyFileEnv)
        where TResource : IResourceWithEnvironment
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Export the ASP.NET Core HTTPS devlopment certificate & private key to PEM files, bind mount them into the container
            // and configure it to use them via the specified environment variables.
            var (certPath, certKeyPath) = ExportDevCertificate(builder.ApplicationBuilder);

            var certFileName = Path.GetFileName(certPath);
            var certKeyFileName = Path.GetFileName(certKeyPath);

            if (builder.Resource is ContainerResource containerResource)
            {
                const string DEV_CERT_BIND_MOUNT_DEST_DIR = "/dev-certs";

                var bindSource = Path.GetDirectoryName(certPath) ?? throw new UnreachableException();

                var certFileDest = Path.Combine(DEV_CERT_BIND_MOUNT_DEST_DIR, certFileName);
                var certKeyFileDest = Path.Combine(DEV_CERT_BIND_MOUNT_DEST_DIR, certKeyFileName);

                builder.ApplicationBuilder.CreateResourceBuilder(containerResource)
                    .WithBindMount(bindSource, DEV_CERT_BIND_MOUNT_DEST_DIR, isReadOnly: true)
                    .WithEnvironment(certFileEnv, certFileDest)
                    .WithEnvironment(certKeyFileEnv, certKeyFileDest);
            }
            else
            {
                builder
                    .WithEnvironment(certFileEnv, certPath)
                    .WithEnvironment(certKeyFileEnv, certKeyPath);
            }
        }

        return builder;
    }

    private static (string, string) ExportDevCertificate(IDistributedApplicationBuilder builder)
    {
        // Exports the ASP.NET Core HTTPS development certificate & private key to PEM files using 'dotnet dev-certs https' to a temporary
        // directory and returns the path.
        // TODO: Check if we're running on a platform that already has the cert and key exported to a file (e.g. macOS) and just use those intead.
        var appNameHash = builder.Configuration["AppHost:Sha256"]![..10];
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire.{appNameHash}");
        var certExportPath = Path.Combine(tempDir, "dev-cert.pem");
        var certKeyExportPath = Path.Combine(tempDir, "dev-cert.key");

        if (File.Exists(certExportPath) && File.Exists(certKeyExportPath))
        {
            // Certificate already exported, return the path.
            return (certExportPath, certKeyExportPath);
        }

        if (File.Exists(certExportPath))
        {
            File.Delete(certExportPath);
        }

        if (File.Exists(certKeyExportPath))
        {
            File.Delete(certKeyExportPath);
        }

        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        string[] args = ["dev-certs", "https", "--export-path", $"\"{certExportPath}\"", "--format", "Pem", "--no-password"];
        var argsString = string.Join(' ', args);

        var exportProcess = Process.Start("dotnet", argsString);

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

public class DevCertAnnotation
{

}
