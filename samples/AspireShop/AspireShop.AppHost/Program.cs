var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin(c => c.WithHostPort(15180));

var catalogDb = postgres.AddDatabase("catalogdb");

var basketCache = builder.AddRedis("basketcache")
    .WithDataVolume()
    .WithRedisCommander(c => c.WithHostPort(15181));

var catalogDbManager = builder.AddProject<Projects.AspireShop_CatalogDbManager>("catalogdbmanager")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithHttpHealthCheck("/health");

var catalogService = builder.AddProject<Projects.AspireShop_CatalogService>("catalogservice")
    .WithReference(catalogDb)
    .WaitFor(catalogDbManager);

var basketService = builder.AddProject<Projects.AspireShop_BasketService>("basketservice")
    .WithReference(basketCache)
    .WaitFor(basketCache);

builder.AddProject<Projects.AspireShop_Frontend>("frontend")
    .WithExternalHttpEndpoints()
    .WithReference(basketService)
    .WithReference(catalogService)
    .WaitFor(catalogService);

builder.Build().Run();
