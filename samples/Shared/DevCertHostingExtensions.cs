using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        this IResourceBuilder<TResource> builder, CertificateFileFormat certificateFileFormat, string certFileEnv, string certPasswordOrKeyEnv, Action<string, string>? onSuccessfulExport = null)
        where TResource : IResourceWithEnvironment
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode && builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>(async (e, ct) =>
            {
                var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(builder.Resource);

                // Export the ASP.NET Core HTTPS development certificate & private key to files and configure the resource to use them via
                // the specified environment variables.
                var (exported, certPath, certKeyPath) = await TryExportDevCertificateAsync(certificateFileFormat, builder.ApplicationBuilder, logger);

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
                    
                    var containerBuilder = builder.ApplicationBuilder.CreateResourceBuilder(containerResource)
                        .WithBindMount(bindSource, DEV_CERT_BIND_MOUNT_DEST_DIR, isReadOnly: true);
                    if (certPfxFileEnv is not null && certPasswordEnv is not null)
                    {
                        containerBuilder
                            .WithEnvironment(certPfxFileEnv, certFileDest)
                            .WithEnvironment(certPasswordEnv, "password");;
                    }
                    else
                    {
                        containerBuilder
                            .WithEnvironment(certFileEnv, certFileDest)
                            .WithEnvironment(certKeyFileEnv, certKeyFileDest);
                    }
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

    private static async Task<(bool, string CertFilePath, string? CertPasswordOrKeyFilePath)> TryExportDevCertificateAsync(CertificateFileFormat certFileMode, IDistributedApplicationBuilder builder, ILogger logger)
    {
        // Exports the ASP.NET Core HTTPS development certificate using 'dotnet dev-certs https' to a directory and returns the path.
        var certDir = GetOrCreateAppHostCertDirectory(builder);
        var certExportPath = Path.Join(certDir, certFileMode == CertificateFileFormat.Pem ? "dev-cert.pem" : "dev-cert.pfx");
        var certKeyExportPath = Path.Join(certDir, "dev-cert.key");

        if (certFileMode is CertificateFileFormat.Pfx or CertificateFileFormat.PfxWithPassword && File.Exists(certExportPath))
        {
            // Certificate already exported, return the path and password (if requested).
            logger.LogDebug("Using previously exported dev cert PFX file '{CertPath}'", certExportPath);
            // TODO: Get password from user secrets
            return (true, certExportPath, "password", null);
        }

        if (File.Exists(certExportPath) && (certFileMode == CertificateFileFormat.Pfx || File.Exists(certKeyExportPath)))
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
        var assemblyMetadata = builder.AppHostAssembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
        var projectDir = GetMetadataValue(assemblyMetadata, "AppHostProjectPath");
        var objDir = GetMetadataValue(assemblyMetadata, "AppHostProjectBaseIntermediateOutputPath");
        var dirPath = projectDir is not null && objDir is not null
            ? Path.Join(projectDir, objDir, "aspire")
            : Directory.CreateTempSubdirectory(GetAppHostSpecificTempDirPrefix(builder)).FullName;

        // Create the directory
        Directory.CreateDirectory(dirPath);

        return dirPath;
    }

    private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key) =>
        assemblyMetadata?.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;

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
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];

                s[i] = _invalidPathChars.Contains(c) ? '_' : c;
            }
        });
    }
}

public enum CertificateFileFormat
{
    Pfx,
    PfxWithPassword,
    Pem,
}
