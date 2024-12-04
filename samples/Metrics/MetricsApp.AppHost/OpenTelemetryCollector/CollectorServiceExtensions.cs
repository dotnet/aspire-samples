using Aspire.Hosting.Lifecycle;

namespace MetricsApp.AppHost.OpenTelemetryCollector;

internal static class CollectorServiceExtensions
{
    public static IDistributedApplicationBuilder AddCollectorInfrastructure(this IDistributedApplicationBuilder builder)
    {
        builder.Services.TryAddLifecycleHook<CollectorLifecycleHook>();

        return builder;
    }
}
