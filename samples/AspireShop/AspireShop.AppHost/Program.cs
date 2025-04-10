var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

if (builder.ExecutionContext.IsRunMode)
{
    // Data volumes don't work on ACA for Postgres so only add when running
    postgres.WithDataVolume();
}

var catalogDb = postgres.AddDatabase("catalogdb");

var basketCache = builder.AddRedis("basketcache")
    .WithDataVolume()
    .WithRedisCommander();

var catalogDbManager = builder.AddProject<Projects.AspireShop_CatalogDbManager>("catalogdbmanager")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithHttpHealthCheck("/health")
    .WithHttpCommand("/reset-db", "Reset Database", commandOptions: new() { IconName = "DatabaseLightning" });

var catalogService = builder.AddProject<Projects.AspireShop_CatalogService>("catalogservice")
    .WithReference(catalogDb)
    .WaitFor(catalogDbManager)
    .WithHttpHealthCheck("/health");

var basketService = builder.AddProject<Projects.AspireShop_BasketService>("basketservice")
    .WithReference(basketCache)
    .WaitFor(basketCache);

builder.AddProject<Projects.AspireShop_Frontend>("frontend")
    .WithExternalHttpEndpoints()
    .WithUrlForEndpoint("https", url => url.DisplayText = "Online Store (HTTPS)")
    .WithUrlForEndpoint("http", url => url.DisplayText = "Online Store (HTTP)")
    .WithHttpHealthCheck("/health")
    .WithReference(basketService)
    .WithReference(catalogService)
    .WaitFor(catalogService);

builder.Build().Run();
