var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgres("catalog").AddDatabase("catalogdb");
var basketCache = builder.AddRedis("basketcache");

var catalogService = builder.AddProject<Projects.AspireShop_CatalogService>("catalogservice")
    .WithReference(catalogDb);

var basketService = builder.AddProject<Projects.AspireShop_BasketService>("basketservice")
    .WithReference(basketCache);

var frontend = builder.AddProject<Projects.AspireShop_Frontend>("frontend")
    .WithReference(basketService)
    .WithReference(catalogService)
    .WithExternalHttpEndpoints();

var dbmgr = builder.AddProject<Projects.AspireShop_CatalogDbManager>("catalogdbmanager")
    .WithReference(catalogDb);

builder.Build().Run();
