// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SamplesIntegrationTests;

/// <summary>
/// Verifies that the Aspire publish manifest generated for each sample AppHost matches the
/// committed "expected" manifest (the <c>aspire-manifest.json</c> file in the AppHost project
/// directory). This catches unintended changes to the output of <c>aspire publish</c>.
/// </summary>
/// <remarks>
/// When a change intentionally affects a manifest, regenerate the committed manifests by running
/// the tests locally with the <c>ASPIRE_UPDATE_MANIFESTS</c> environment variable set to
/// <c>true</c>, then review and commit the updated <c>aspire-manifest.json</c> files:
/// <list type="bullet">
///   <item><description>PowerShell: <c>$env:ASPIRE_UPDATE_MANIFESTS='true'; dotnet test --filter "FullyQualifiedName~ManifestTests"</c></description></item>
///   <item><description>bash: <c>ASPIRE_UPDATE_MANIFESTS=true dotnet test --filter "FullyQualifiedName~ManifestTests"</c></description></item>
/// </list>
/// Update mode is intentionally disabled when running on CI.
/// </remarks>
[Collection(ManifestTestsCollection.Name)]
public class ManifestTests(ITestOutputHelper testOutput)
{
    private static readonly TimeSpan GenerateTimeout = TimeSpan.FromMinutes(5);

    private const string ExpectedManifestFileName = "aspire-manifest.json";
    private const string GeneratedManifestFileName = "aspire-manifest.g.json";
    private const string VolumeHashPlaceholder = "<sha256>";

    // Files written next to the manifest as a side effect of publishing that must not be committed.
    private static readonly string[] GeneratedArtifactPatterns = ["*.module.bicep", "*.Dockerfile"];

    // Some samples bake ambient configuration values into resource environment variables (e.g. the
    // Metrics sample's OpenTelemetry collector reads the OTLP API key from the "AppHost:OtlpApiKey"
    // configuration value). Pin those inputs to deterministic values during generation so the
    // manifests are reproducible regardless of the contributor's environment, and so a real OTLP API
    // key can never leak into a committed manifest.
    //
    // The API key must be neutralized with a fixed, non-empty placeholder rather than null or an empty
    // string: on Windows, setting an environment variable to null or "" deletes it, which would let a
    // real value supplied via user secrets flow into the manifest. A non-empty override reliably wins
    // over user secrets because environment variables have higher precedence in the configuration chain.
    private const string NeutralizedOtlpApiKey = "neutralized-otlp-api-key";

    private static readonly (string Key, string? Value)[] NeutralizedConfiguration =
    [
        ("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:18889"),
        ("AppHost__OtlpApiKey", NeutralizedOtlpApiKey),
    ];

    [Theory]
    [MemberData(nameof(AppHostAssemblies))]
    public async Task ManifestMatchesExpected(string appHostPath)
    {
        if (IsUpdateRequested && IsContinuousIntegration)
        {
            Assert.Fail($"'{UpdateEnvironmentVariable}' must not be set when running on CI; expected manifests can only be regenerated locally.");
        }

        var appHostAssembly = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, appHostPath));
        var appHostType = appHostAssembly.GetTypes().FirstOrDefault(t => t.Name.EndsWith("_AppHost", StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Could not find the AppHost entry point type in '{appHostPath}'.");
        var appHostDir = GetAppHostDirectory(appHostAssembly);
        var appPrefix = GetVolumeNamePrefix(appHostAssembly);

        var actualManifest = await GenerateManifestAsync(appHostType, appHostDir, appPrefix);
        var expectedManifestPath = Path.Combine(appHostDir, ExpectedManifestFileName);

        if (IsUpdateRequested)
        {
            await File.WriteAllTextAsync(expectedManifestPath, actualManifest);
            testOutput.WriteLine($"Updated expected manifest: {expectedManifestPath}");
            return;
        }

        if (!File.Exists(expectedManifestPath))
        {
            Assert.Fail($"No expected manifest was found at '{expectedManifestPath}'.{Environment.NewLine}{RegenerateInstructions}");
        }

        var expectedManifest = Normalize(await File.ReadAllTextAsync(expectedManifestPath), appPrefix);

        if (expectedManifest != actualManifest)
        {
            testOutput.WriteLine(RegenerateInstructions);
        }

        Assert.Equal(expectedManifest, actualManifest);
    }

    private async Task<string> GenerateManifestAsync(Type appHostType, string appHostDir, string appPrefix)
    {
        // Snapshot the directory so cleanup only ever removes files this run created, never
        // pre-existing source files that happen to match a generated-artifact pattern.
        var preexistingFiles = Directory.GetFiles(appHostDir).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Clear any artifacts left behind by a previously interrupted run before generating.
        CleanGeneratedArtifacts(appHostDir, preexistingFiles);

        var generatedManifestPath = Path.Combine(appHostDir, GeneratedManifestFileName);
        using var cts = new CancellationTokenSource(GenerateTimeout);
        using var _ = NeutralizeConfiguration();

        try
        {
            // Generate the manifest into the AppHost's own directory so that all relative paths
            // (project references, bind mounts, generated bicep/Dockerfiles) are machine-independent.
            // Publishing the manifest runs in-process and does not require Docker.
            var builder = await DistributedApplicationTestingBuilder.CreateAsync(
                appHostType,
                ["--operation", "publish", "--publisher", "manifest", "--output-path", generatedManifestPath],
                cts.Token);

            // Route the AppHost logs to the test output so a failed publish - which stops the in-process
            // host without faulting RunAsync - surfaces its underlying error instead of silently
            // producing a missing or truncated manifest. Warning level keeps successful runs quiet while
            // still capturing the publishing pipeline error that explains a validation failure below.
            builder.Services.AddLogging(logging =>
            {
                logging.AddXUnit(testOutput);
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            await using var app = await builder.BuildAsync(cts.Token);
            await app.RunAsync(cts.Token);

            var manifest = File.Exists(generatedManifestPath)
                ? await File.ReadAllTextAsync(generatedManifestPath, cts.Token)
                : string.Empty;

            EnsureManifestIsValid(manifest, appHostType);

            return Normalize(manifest, appPrefix);
        }
        finally
        {
            CleanGeneratedArtifacts(appHostDir, preexistingFiles);
        }
    }

    /// <summary>
    /// Fails the test when the publish operation did not produce a complete, valid manifest. A failed
    /// publishing pipeline stops the in-process host without faulting
    /// <see cref="DistributedApplication.RunAsync(CancellationToken)"/>, so the manifest can be missing
    /// or truncated even though the test host exited normally. Without this guard a broken sample would
    /// silently pass (and, in update mode, overwrite the committed manifest with invalid JSON).
    /// </summary>
    private static void EnsureManifestIsValid(string manifest, Type appHostType)
    {
        var appHostName = appHostType.Assembly.GetName().Name;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(manifest);
        }
        catch (JsonException ex)
        {
            throw new XunitException(
                $"The publish manifest generated for '{appHostName}' is not valid JSON, which almost always means " +
                $"'aspire publish' failed. Check the test output above for the underlying publish error, then fix it " +
                $"before regenerating the expected manifest.{Environment.NewLine}JSON error: {ex.Message}");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("resources", out var resources)
                || resources.ValueKind != JsonValueKind.Object)
            {
                throw new XunitException(
                    $"The publish manifest generated for '{appHostName}' does not contain a 'resources' object, which " +
                    $"almost always means 'aspire publish' failed. Check the test output above for the underlying " +
                    $"publish error, then fix it before regenerating the expected manifest.");
            }
        }
    }

    private static void CleanGeneratedArtifacts(string appHostDir, HashSet<string> preexistingFiles)
    {
        var generatedManifest = Path.Combine(appHostDir, GeneratedManifestFileName);
        if (File.Exists(generatedManifest))
        {
            File.Delete(generatedManifest);
        }

        foreach (var pattern in GeneratedArtifactPatterns)
        {
            foreach (var file in Directory.GetFiles(appHostDir, pattern))
            {
                if (!preexistingFiles.Contains(file))
                {
                    File.Delete(file);
                }
            }
        }
    }

    private static IDisposable NeutralizeConfiguration()
    {
        var previousValues = new (string Key, string? Value)[NeutralizedConfiguration.Length];
        for (var i = 0; i < NeutralizedConfiguration.Length; i++)
        {
            var (key, value) = NeutralizedConfiguration[i];
            previousValues[i] = (key, Environment.GetEnvironmentVariable(key));
            Environment.SetEnvironmentVariable(key, value);
        }

        return new ConfigurationScope(previousValues);
    }

    private sealed class ConfigurationScope((string Key, string? Value)[] previousValues) : IDisposable
    {
        public void Dispose()
        {
            foreach (var (key, value) in previousValues)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string Normalize(string manifest, string appPrefix)
    {
        // Volume names embed a 10-character SHA-256 prefix derived from the absolute AppHost path,
        // which differs from machine to machine. Replace it with a stable placeholder.
        if (!string.IsNullOrEmpty(appPrefix))
        {
            manifest = Regex.Replace(
                manifest,
                Regex.Escape(appPrefix) + "-[0-9a-f]{10}-",
                $"{appPrefix}-{VolumeHashPlaceholder}-");
        }

        // Normalize line endings so manifests compare equal regardless of git core.autocrlf settings.
        return manifest.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private static string GetAppHostDirectory(Assembly appHostAssembly)
    {
        var path = appHostAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(m => m.Key.Equals("apphostprojectpath", StringComparison.OrdinalIgnoreCase))?.Value;

        return !string.IsNullOrEmpty(path) && Directory.Exists(path)
            ? path
            : throw new InvalidOperationException(
                $"Could not resolve the AppHost project directory for '{appHostAssembly.GetName().Name}'.");
    }

    // Mirrors Aspire's VolumeNameGenerator: the volume name prefix is the sanitized, lower-cased application name.
    private static string GetVolumeNamePrefix(Assembly appHostAssembly)
    {
        var applicationName = appHostAssembly.GetName().Name ?? string.Empty;
        var builder = new StringBuilder(applicationName.Length);

        for (var i = 0; i < applicationName.Length; i++)
        {
            var c = applicationName[i];
            var isValid = i == 0
                ? char.IsAsciiLetter(c) || char.IsAsciiDigit(c)
                : char.IsAsciiLetter(c) || char.IsAsciiDigit(c) || c is '_' or '.' or '-';
            builder.Append(isValid ? c : '_');
        }

        return builder.ToString().ToLowerInvariant();
    }

    public static TheoryData<string> AppHostAssemblies() =>
        [.. Directory.GetFiles(AppContext.BaseDirectory, "*.AppHost.dll")
            .Where(fileName => !fileName.EndsWith("Aspire.Hosting.AppHost.dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(AppContext.BaseDirectory, path))];

    private const string UpdateEnvironmentVariable = "ASPIRE_UPDATE_MANIFESTS";

    private static bool IsUpdateRequested =>
        string.Equals(Environment.GetEnvironmentVariable(UpdateEnvironmentVariable), "true", StringComparison.OrdinalIgnoreCase);

    private static bool IsContinuousIntegration =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    private static string RegenerateInstructions =>
        $"To regenerate the expected manifests after an intentional change, run the tests locally with " +
        $"the '{UpdateEnvironmentVariable}' environment variable set to 'true', then review and commit the " +
        $"updated '{ExpectedManifestFileName}' files.";
}

[CollectionDefinition(Name, DisableParallelization = true)]
public class ManifestTestsCollection
{
    // Manifests are generated into the AppHost source directories, so the tests must not run in
    // parallel with each other or with other tests that touch those directories.
    public const string Name = "Manifest tests";
}
