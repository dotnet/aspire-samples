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

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithDataVolume(isReadOnly: false)
    .WithManagementPlugin();

var basketCache = builder.AddRedis("basketcache")
    .WithDataVolume()
    .WithRedisCommander();

var catalogDbManager = builder.AddProject<Projects.AspireShop_CatalogDbManager>("catalogdbmanager")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithHttpHealthCheck("/health")
    .WithHttpsCommand("/reset-db", "Reset Database", iconName: "DatabaseLightning");

var catalogService = builder.AddProject<Projects.AspireShop_CatalogService>("catalogservice")
    .WithReference(catalogDb)
    .WaitFor(catalogDbManager);

var basketService = builder.AddProject<Projects.AspireShop_BasketService>("basketservice")
    .WithReference(basketCache)
    .WithReference(rabbitmq)
    .WaitFor(basketCache)
    .WaitFor(rabbitmq);

//workers are not referenced as they not dependencies of anything
builder.AddProject<Projects.AspireShop_BasketWorker>("basketworker")
    .WithReference(basketCache)
    .WithReference(rabbitmq)
    .WaitFor(basketCache)
    .WaitFor(rabbitmq);

builder.AddProject<Projects.AspireShop_Frontend>("frontend")
    .WithExternalHttpEndpoints()
    .WithReference(basketService)
    .WithReference(catalogService)
    .WaitFor(catalogService);

builder.Build().Run();
