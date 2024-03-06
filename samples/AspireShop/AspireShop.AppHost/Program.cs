using Aspire.Hosting.Lifecycle;

var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgres("catalog").AddDatabase("catalogdb");
var basketCache = builder.AddRedis("basketcache");

var catalogService = builder.AddProject<Projects.AspireShop_CatalogService>("catalogservice")
    .WithReference(catalogDb);

var basketService = builder.AddProject<Projects.AspireShop_BasketService>("basketservice")
    .WithReference(basketCache)
    // Force http2 for gRPC
    .WithEndpoint("http", ep => ep.Transport = "http2")
    .WithEndpoint("https", ep => ep.Transport = "http2");

builder.AddProject<Projects.AspireShop_Frontend>("frontend")
    .WithReference(basketService)
    .WithReference(catalogService);

builder.AddProject<Projects.AspireShop_CatalogDbManager>("catalogdbmanager")
    .WithReference(catalogDb);

builder.Services.AddLifecycleHook<FixupsLifecycleHook>();

builder.Build().Run();

public class FixupsLifecycleHook : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        foreach (var project in appModel.GetProjectResources())
        {
            // Enabled forwarded headers
            project.Annotations.Add(new EnvironmentCallbackAnnotation(c =>
            {
                if (c.ExecutionContext.IsPublishMode)
                {
                    c.EnvironmentVariables["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true";
                }
            }));

            // Workaround default endpoints issue
            if (project.TryGetEndpoints(out var endpoints))
            {
                foreach (var endpoint in endpoints)
                {
                    if (endpoint.Transport == "tcp" && (endpoint.UriScheme == "http" || endpoint.UriScheme == "https"))
                    {
                        endpoint.Transport = "http";
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
}
