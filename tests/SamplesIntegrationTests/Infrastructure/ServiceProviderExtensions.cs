using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SamplesIntegrationTests.Infrastructure;

internal static class ServiceProviderExtensions
{
    public static object GetRequiredService(this IServiceProvider serviceProvider, string typeName, Assembly assembly)
    {
        var serviceType = assembly.GetType(typeName, throwOnError: true)
            ?? throw new InvalidOperationException($"Type {typeName} in assembly {assembly.FullName} was not found.");

        var service = serviceProvider.GetRequiredService(serviceType);

        return service;
    }
}
