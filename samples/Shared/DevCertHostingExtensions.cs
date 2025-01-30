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
        this IResourceBuilder<TResource> builder, CertificateFileFormat certificateFileFormat, string certFileEnv, string? certPasswordOrKeyEnv, Func<IServiceProvider, string, string?, Task>? onSuccessfulExport = null)
        where TResource : IResourceWithEnvironment
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certFileEnv);
        if (certificateFileFormat is CertificateFileFormat.PfxWithPassword && string.IsNullOrWhiteSpace(certPasswordOrKeyEnv))
        {
            throw new ArgumentException("The environment variable name for the certificate password must be provided when exporting a PFX certificate with a password.", nameof(certPasswordOrKeyEnv));
        }
        if (certificateFileFormat is CertificateFileFormat.Pem && string.IsNullOrWhiteSpace(certPasswordOrKeyEnv))
        {
            throw new ArgumentException("The environment variable name for the certificate key file must be provided when exporting a PEM certificate.", nameof(certPasswordOrKeyEnv));
        }

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode && builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            // This event callback will run before the application starts. If multiple resources are running with the dev cert and thus multiples of this callback are registered,
            // the call to TryExportDevCertificateAsync will ensure that the required certificate formats will only be exported once, each, as required. The callbacks are run
            // seqentially in order so no need to lock or synchronize access.
            builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>(async (e, ct) =>
            {
                var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(builder.Resource);

                // Export the ASP.NET Core HTTPS development certificate & private key to files and configure the resource to use them via
                // the specified environment variables.
                var (exported, certPath, certPasswordOrKeyPath) = await TryExportDevCertificateAsync(certificateFileFormat, builder.ApplicationBuilder, logger);

                if (!exported)
                {
                    // The export failed for some reason, don't configure the resource to use the certificate.
                    return;
                }

                var certPassword = certificateFileFormat is CertificateFileFormat.PfxWithPassword ? certPasswordOrKeyPath : null;

                if (builder.Resource is ContainerResource containerResource)
                {
                    // Bind-mount the certificate files into the container.
                    const string DEV_CERT_BIND_MOUNT_DEST_DIR = "/dev-certs";

                    var certFileName = Path.GetFileName(certPath);
                    var certKeyFileName = certificateFileFormat is CertificateFileFormat.Pem ? Path.GetFileName(certPasswordOrKeyPath) : null;

                    var bindSource = Path.GetDirectoryName(certPath) ?? throw new UnreachableException();

                    var certFileDest = $"{DEV_CERT_BIND_MOUNT_DEST_DIR}/{certFileName}";
                    var certKeyFileDest = certKeyFileName is not null ? $"{DEV_CERT_BIND_MOUNT_DEST_DIR}/{certKeyFileName}" : null;
                    
                    var containerBuilder = builder.ApplicationBuilder.CreateResourceBuilder(containerResource)
                        .WithBindMount(bindSource, DEV_CERT_BIND_MOUNT_DEST_DIR, isReadOnly: true)
                        .WithEnvironment(certFileEnv, certFileDest);

                    if (certPasswordOrKeyEnv is not null)
                    {
                        if (certPassword is not null)
                        {
                            containerBuilder.WithEnvironment(certPasswordOrKeyEnv, certPassword);
                        }
                        else if (certKeyFileDest is not null)
                        {
                            containerBuilder.WithEnvironment(certPasswordOrKeyEnv, certKeyFileDest);
                        }
                    }
                }
                else
                {
                    // Set environment variable for the certificate file.
                    builder.WithEnvironment(certFileEnv, certPath);

                    // Set environment variable for the certificate password or key file.
                    if (certPasswordOrKeyEnv is not null)
                    {
                        if (certPassword is not null)
                        {
                            builder.WithEnvironment(certPasswordOrKeyEnv, certPassword);
                        }
                        else if (certPasswordOrKeyPath is not null)
                        {
                            builder.WithEnvironment(certPasswordOrKeyEnv, certPasswordOrKeyPath);
                        }
                    }
                }

                if (onSuccessfulExport is not null)
                {
                    await onSuccessfulExport(e.Services, certPath, certPasswordOrKeyPath);
                }
            });
        }

        return builder;
    }

    /// <summary>
    /// Tries to export the ASP.NET Core HTTPS development certificate to a file for the current app host and returns the details.
    /// </summary>
    public static async Task<(bool ExportSuccessful, string CertFilePath, string? CertPasswordOrKeyFilePath)> TryExportDevCertificateAsync(CertificateFileFormat certFileMode, IDistributedApplicationBuilder builder, ILogger logger)
    {
        // Exports the ASP.NET Core HTTPS development certificate using 'dotnet dev-certs https' to a directory and returns the path.
        var certDir = GetOrCreateAppHostCertDirectory(builder);
        var certExportPath = Path.Join(certDir, certFileMode switch
            {
                CertificateFileFormat.Pem =>"dev-cert.pem",
                CertificateFileFormat.Pfx => "dev-cert.pfx",
                CertificateFileFormat.PfxWithPassword => "dev-cert-pw.pfx",
                _ => throw new ArgumentOutOfRangeException(nameof(certFileMode)),
            });
        var certKeyExportPath = certFileMode is CertificateFileFormat.Pem ? Path.Join(certDir, "dev-cert.key") : null;
        const string passwordName = "dev-cert-password";

        if (certFileMode is CertificateFileFormat.PfxWithPassword && File.Exists(certExportPath))
        {
            // Certificate already exported, return the path and password.
            logger.LogDebug("Using previously exported dev cert PFX file '{CertPath}'", certExportPath);
            var passwordValue = builder.Configuration[$"Parameters:{passwordName}"];
            if (!string.IsNullOrEmpty(passwordValue))
            {
                return (true, certExportPath, passwordValue);
            }
            logger.LogWarning($"The dev cert password is required but was not found in the configuration with key 'Parameters:{passwordName}'. Deleting existing dev cert file and re-exporting.");
            File.Delete(certExportPath);
        }
        else if (certFileMode is CertificateFileFormat.Pfx & File.Exists(certExportPath))
        {
            // Certificate already exported, return the path.
            logger.LogDebug("Using previously exported dev cert PFX file '{CertPath}'", certExportPath);
            return (true, certExportPath, null);
        }
        else if (certFileMode == CertificateFileFormat.Pem)
        {
            if (File.Exists(certExportPath) && File.Exists(certKeyExportPath))
            {
                // Certificate already exported, return the path.
                logger.LogDebug("Using previously exported dev cert PEM file '{CertPath}' and key file '{CertKeyPath}'", certExportPath, certKeyExportPath);
                return (true, certExportPath, certKeyExportPath);
            }

            if (File.Exists(certExportPath))
            {
                logger.LogTrace("Previously exported key file is present but dev cert file '{CertPath}' is missing. Deleting dev cert file and re-exporting.", certExportPath);
                File.Delete(certExportPath);
            }

            if (File.Exists(certKeyExportPath))
            {
                logger.LogTrace("Previously exported dev cert file is present but key file '{CertKeyPath}' is missing. Deleting key file and re-exporting.", certKeyExportPath);
                File.Delete(certKeyExportPath);
            }
        }

        var generatedPassword = certFileMode is CertificateFileFormat.PfxWithPassword
            ? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, passwordName).Default?.GetDefaultValue()
                ?? throw new InvalidOperationException($"The default value for the parameter '{passwordName}' was not set.")
            : null;
        var passwordArgs = generatedPassword is null ? "--no-password" : $"--password \"{generatedPassword}\"";
        var formatArg = certFileMode is CertificateFileFormat.Pem ? "Pem" : "Pfx";
        string[] args = ["dev-certs", "https", "--export-path", $"\"{certExportPath}\"", "--format", formatArg, passwordArgs];
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

            if (exited && certFileMode is CertificateFileFormat.Pfx or CertificateFileFormat.PfxWithPassword && File.Exists(certExportPath))
            {
                logger.LogDebug("Dev cert exported to '{CertPath}'", certExportPath);
                return (true, certExportPath, certFileMode is CertificateFileFormat.PfxWithPassword ? generatedPassword : null);
            }

            if (exited && certFileMode is CertificateFileFormat.Pem && File.Exists(certExportPath) && File.Exists(certKeyExportPath))
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
