using System.Diagnostics;
using System.Reflection;

namespace SamplesIntegrationTests;

internal static partial class DistributedApplicationTestFactory
{
    public static async Task<IDistributedApplicationTestingBuilder> CreateAsync(string appHostAssemblyPath, TextWriter? outputWriter)
    {
        var appHostProjectName = Path.GetFileNameWithoutExtension(appHostAssemblyPath) ?? throw new InvalidOperationException("AppHost assembly was not found.");

        var appHostAssembly = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, appHostAssemblyPath));

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
        if (outputWriter is not null)
        {
            builder.WriteOutputTo(outputWriter);
        }

        return builder;
    }
}
