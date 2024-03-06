var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.HealthChecksUI_ApiService>("apiservice");

var webFrontend = builder.AddProject<Projects.HealthChecksUI_Web>("webfrontend")
    .WithReference(cache)
    .WithReference(apiService)
    .AsExternal();

builder.AddHealthChecksUI("healthchecksui")
    .WithReference(apiService)
    .WithReference(webFrontend);

builder.Build().Run();
