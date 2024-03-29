using System.Diagnostics;
using System.Reflection;
using Xunit.Abstractions;

namespace SamplesIntegrationTests;

internal static class DistributedApplicationTestFactory
{
    public static async Task<IDistributedApplicationTestingBuilder> CreateAsync(string appHostProjectPath, ITestOutputHelper testOutput)
    {
        var appHostProjectName = Path.GetFileNameWithoutExtension(appHostProjectPath) ?? throw new InvalidOperationException("AppHost project was not found.");
        var appHostProjectDirectory = Path.GetDirectoryName(appHostProjectPath) ?? throw new InvalidOperationException("Directory for AppHost project was not found.");
#if DEBUG
        var configuration = "Debug";
#else
        var configuration = "Release";
#endif
        // TODO: Handle different assets output path
        var appHostAssembly = Assembly.LoadFrom(Path.Combine(appHostProjectDirectory, "bin", configuration, "net8.0", $"{appHostProjectName}.dll"));

        var appHostType = appHostAssembly.GetTypes().FirstOrDefault(t => t.Name.EndsWith("_AppHost"))
            ?? throw new InvalidOperationException("Generated AppHost type not found.");

        var createAsyncMethod = typeof(DistributedApplicationTestingBuilder).GetMethod(nameof(DistributedApplicationTestingBuilder.CreateAsync))
            ?? throw new InvalidOperationException("DistributedApplicationTestingBuilder.CreateAsync method not found.");

        var createAsyncConcrete = createAsyncMethod.MakeGenericMethod(appHostType);

        var testBuilderTask = createAsyncConcrete.Invoke(null, [CancellationToken.None]) as Task<IDistributedApplicationTestingBuilder>
            ?? throw new UnreachableException();

        var builder = await testBuilderTask;

        builder.WithRandomParameterValues();
        builder.WithAnonymousVolumeNames();
        builder.WriteOutputTo(testOutput);

        return builder;
    }
}
