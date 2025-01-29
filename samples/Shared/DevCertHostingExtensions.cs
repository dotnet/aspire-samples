using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

public static class DevCertHostingExtensions
{
    /// <summary>
    /// Injects the ASP.NET Core HTTPS developer certificate into the resource via the specified environment variables when
    /// <paramref name="builder"/>.<see cref="IResourceBuilder{T}.ApplicationBuilder">ApplicationBuilder</see>.<see cref="IDistributedApplicationBuilder.ExecutionContext">ExecutionContext</see>.<see cref="DistributedApplicationExecutionContext.IsRunMode">IsRunMode</see><c> == true</c>.<br/>
    /// If the resource is a <see cref="ContainerResource"/>, the certificate files will be bind mounted into the container.
    /// </summary>
    /// <remarks>
    /// This method <strong>does not</strong> configure an HTTPS endpoint on the resource.
    /// Use <see cref="ResourceBuilderExtensions.WithHttpsEndpoint{TResource}"/> to configure an HTTPS endpoint.
    /// </remarks>
    public static IResourceBuilder<TResource> RunWithHttpsDevCertificate<TResource>(
        this IResourceBuilder<TResource> builder, string certFileEnv, string certKeyFileEnv, Action<string, string>? onSuccessfulExport = null)
        where TResource : IResourceWithEnvironment
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode && builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>(async (e, ct) =>
            {
                var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(builder.Resource);

                // Export the ASP.NET Core HTTPS development certificate & private key to files and configure the resource to use them via
                // the specified environment variables.
                var (exported, certPath, certKeyPath) = await TryExportDevCertificateAsync(builder.ApplicationBuilder, logger);

                if (!exported)
                {
                    // The export failed for some reason, don't configure the resource to use the certificate.
                    return;
                }

                if (builder.Resource is ContainerResource containerResource)
                {
                    // Bind-mount the certificate files into the container.
                    const string DEV_CERT_BIND_MOUNT_DEST_DIR = "/dev-certs";

                    var certFileName = Path.GetFileName(certPath);
                    var certKeyFileName = Path.GetFileName(certKeyPath);

                    var bindSource = Path.GetDirectoryName(certPath) ?? throw new UnreachableException();

                    var certFileDest = $"{DEV_CERT_BIND_MOUNT_DEST_DIR}/{certFileName}";
                    var certKeyFileDest = $"{DEV_CERT_BIND_MOUNT_DEST_DIR}/{certKeyFileName}";

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

                if (onSuccessfulExport is not null)
                {
                    onSuccessfulExport(certPath, certKeyPath);
                }
            });
        }

        return builder;
    }

    private static async Task<(bool, string CertFilePath, string CertKeyFilPath)> TryExportDevCertificateAsync(IDistributedApplicationBuilder builder, ILogger logger)
    {
        // Exports the ASP.NET Core HTTPS development certificate & private key to PEM files using 'dotnet dev-certs https' to a directory and returns the path.
        var certDir = GetOrCreateAppHostCertDirectory(builder);
        var certExportPath = Path.Combine(certDir, "dev-cert.pem");
        var certKeyExportPath = Path.Combine(certDir, "dev-cert.key");

        if (File.Exists(certExportPath) && File.Exists(certKeyExportPath))
        {
            // Certificate already exported, return the path.
            logger.LogDebug("Using previously exported dev cert files '{CertPath}' and '{CertKeyPath}'", certExportPath, certKeyExportPath);
            return (true, certExportPath, certKeyExportPath);
        }

        if (File.Exists(certExportPath))
        {
            logger.LogTrace("Deleting previously exported dev cert file '{CertPath}'", certExportPath);
            File.Delete(certExportPath);
        }

        if (File.Exists(certKeyExportPath))
        {
            logger.LogTrace("Deleting previously exported dev cert key file '{CertKeyPath}'", certKeyExportPath);
            File.Delete(certKeyExportPath);
        }

        if (!Directory.Exists(certDir))
        {
            logger.LogTrace("Creating directory to export dev cert to '{ExportDir}'", certDir);
            Directory.CreateDirectory(certDir);
        }

        string[] args = ["dev-certs", "https", "--export-path", $"\"{certExportPath}\"", "--format", "Pem", "--no-password"];
        var argsString = string.Join(' ', args);

        logger.LogTrace("Running command to export dev cert: {ExportCmd}", $"dotnet {argsString}");
        var exportStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = argsString,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        var exportProcess = new Process { StartInfo = exportStartInfo };

        Task? stdOutTask = null;
        Task? stdErrTask = null;

        try
        {
            try
            {
                if (exportProcess.Start())
                {
                    stdOutTask = ConsumeOutput(exportProcess.StandardOutput, msg => logger.LogInformation("> {StandardOutput}", msg));
                    stdErrTask = ConsumeOutput(exportProcess.StandardError, msg => logger.LogError("! {ErrorOutput}", msg));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start HTTPS dev certificate export process");
                return default;
            }

            var timeout = TimeSpan.FromSeconds(5);
            var exited = exportProcess.WaitForExit(timeout);

            if (exited && File.Exists(certExportPath) && File.Exists(certKeyExportPath))
            {
                logger.LogDebug("Dev cert exported to '{CertPath}' and '{CertKeyPath}'", certExportPath, certKeyExportPath);
                return (true, certExportPath, certKeyExportPath);
            }

            if (exportProcess.HasExited && exportProcess.ExitCode != 0)
            {
                logger.LogError("HTTPS dev certificate export failed with exit code {ExitCode}", exportProcess.ExitCode);
            }
            else if (!exportProcess.HasExited)
            {
                exportProcess.Kill(true);
                logger.LogError("HTTPS dev certificate export timed out after {TimeoutSeconds} seconds", timeout.TotalSeconds);
            }
            else
            {
                logger.LogError("HTTPS dev certificate export failed for an unknown reason");
            }
            return default;
        }
        finally
        {
            await Task.WhenAll(stdOutTask ?? Task.CompletedTask, stdErrTask ?? Task.CompletedTask);
        }

        static async Task ConsumeOutput(TextReader reader, Action<string> callback)
        {
            char[] buffer = new char[256];
            int charsRead;

            while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                callback(new string(buffer, 0, charsRead));
            }
        }
    }

    private static string GetOrCreateAppHostCertDirectory(IDistributedApplicationBuilder builder)
    {
        // TODO: Check if we're running on a platform that already has the cert and key exported to a file (e.g. macOS) and just use those instead.
        // macOS: Path.Combine(
        //          Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "dev-certs", "https"),
        //          "aspnetcore-localhost-{certificate.Thumbprint}.pfx");
        // linux: CurrentUser/Root store

        // Create a directory in the project's /obj dir or TMP to store the exported certificate and key
        var projectDir = builder.AppHostAssembly?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "apphostprojectpath")
            ?.Value;
        var objDir = builder.AppHostAssembly?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "apphostprojectbasentermediateoutputpath")
            ?.Value;
        var dirPath = projectDir is not null && objDir is not null
            ? Path.Combine(projectDir, objDir, "aspire")
            : Path.Combine(Directory.CreateTempSubdirectory(GetAppHostSpecificTempDirPrefix(builder)).FullName);

        if (!Directory.Exists(dirPath))
        {
            // Create the directory
            Directory.CreateDirectory(dirPath);
        }

        return dirPath;
    }

    private static string GetAppHostSpecificTempDirPrefix(IDistributedApplicationBuilder builder)
    {
        var appName = Sanitize(builder.Environment.ApplicationName).ToLowerInvariant();
        var appNameHash = builder.Configuration["AppHost:Sha256"]![..10].ToLowerInvariant();
        return $"aspire.{appName}.{appNameHash}";
    }

    private static readonly char[] _invalidPathChars = Path.GetInvalidPathChars();

    private static string Sanitize(string name)
    {
        return string.Create(name.Length, name, static (s, name) =>
        {
            var nameSpan = name.AsSpan();

            for (var i = 0; i < nameSpan.Length; i++)
            {
                var c = nameSpan[i];

                s[i] = Array.IndexOf(_invalidPathChars, c) == -1 ? c : '_';
            }
        });
    }
}
