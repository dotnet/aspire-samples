var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedisContainer("cache");

var weatherapi = builder.AddProject<Projects.AspireWithNode_AspNetCoreApi>("weatherapi");

builder.AddNpmApp("frontend", "../NodeFrontend", "watch")
    .WithReference(weatherapi)
    .WithReference(cache)
    .WithServiceBinding(scheme: "http");

builder.Build().Run();
