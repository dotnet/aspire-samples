var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.HealthChecksUI_ApiService>("apiservice");

var webFrontend = builder.AddProject<Projects.HealthChecksUI_Web>("webfrontend")
    .WithReference(cache)
    .WithReference(apiService)
    .WithExternalHttpEndpoints();

builder.AddHealthChecksUI("healthchecksui")
    .WithReference(apiService)
    .WithReference(webFrontend)
    // This will make the HealthChecksUI dashboard available from external networks when deployed.
    // In a production environment, you should consider adding authentication to the ingress layer
    // to restrict access to the dashboard.
    .WithExternalHttpEndpoints();

builder.Build().Run();
