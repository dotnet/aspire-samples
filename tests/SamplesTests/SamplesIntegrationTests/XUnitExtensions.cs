using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SamplesIntegrationTests;

internal static partial class DistributedApplicationTestFactory
{
    public static async Task<IDistributedApplicationTestingBuilder> CreateAsync(string appHostAssemblyPath, ITestOutputHelper testOutputHelper)
    {
        var builder = await CreateAsync(appHostAssemblyPath, new XunitTextWriter(testOutputHelper));
        builder.Services.AddSingleton<ILoggerProvider, XUnitLoggerProvider>();
        builder.Services.AddSingleton(testOutputHelper);
        return builder;
    }
}
