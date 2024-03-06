using Aspire.Hosting.Lifecycle;

var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgres("catalog").AddDatabase("catalogdb");
var basketCache = builder.AddRedis("basketcache");

var catalogService = builder.AddProject<Projects.AspireShop_CatalogService>("catalogservice")
    .WithReference(catalogDb);

var basketService = builder.AddProject<Projects.AspireShop_BasketService>("basketservice")
    .WithReference(basketCache);

builder.AddProject<Projects.AspireShop_Frontend>("frontend")
    .WithReference(basketService)
    .WithReference(catalogService);

builder.AddProject<Projects.AspireShop_CatalogDbManager>("catalogdbmanager")
    .WithReference(catalogDb);

builder.Services.AddLifecycleHook<AspNetCoreForwardedHeadersLifecycleHook>();

builder.Build().Run();

public class AspNetCoreForwardedHeadersLifecycleHook : IDistributedApplicationLifecycleHook
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
        }

        return Task.CompletedTask;
    }
}
