var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.AspireApp33_ApiService>("apiservice");

var webFrontend = builder.AddProject<Projects.AspireApp33_Web>("webfrontend")
    .WithReference(cache)
    .WithReference(apiService);

builder.AddHealthChecksUI("healthchecksui")
    .WithReference(apiService)
    .WithReference(webFrontend, endpointName: "http");

builder.Build().Run();
