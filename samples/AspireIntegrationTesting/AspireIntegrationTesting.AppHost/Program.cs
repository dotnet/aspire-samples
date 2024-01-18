//var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = args, AssemblyName = typeof(Program).Assembly.GetName().Name });
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

builder.AddProject<Projects.AspireIntegrationTesting_ApiService>("apiservice")
    .WithReference(cache);

builder.ApplyResourceFilter();

builder.Build().Run();


public static class Extensions
{
    private static readonly char[] FilterSeparator = [';'];

    public static IDistributedApplicationBuilder ApplyResourceFilter(this IDistributedApplicationBuilder builder, string? filter = null)
    {
        filter ??= builder.Configuration["ResourceFilter"];

        if (filter is null)
        {
            return builder;
        }

        var candidates = filter.Split(FilterSeparator, StringSplitOptions.RemoveEmptyEntries);

        for (int i = builder.Resources.Count - 1; i >= 0; i--)
        {
            var resource = builder.Resources[i];

            if (!candidates.Contains(resource.Name, StringComparer.OrdinalIgnoreCase))
            {
                builder.Resources.Remove(resource);
            }
        }

        return builder;
    }
}
