using System.Globalization;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedisContainer("cache");

var weatherapi = builder.AddProject<Projects.AspireWithNode_AspNetCoreApi>("weatherapi");

builder.AddExecutable("frontend", "npm", "../NodeFrontend", "run", "watch")
    .WithReference(weatherapi)
    .WithReference(cache)
    .WithOtlpExporter()
    .WithEnvironment("NODE_ENV", builder.Environment.IsDevelopment() ? "development" : "production")
    .WithEnvironment("PORT", "{{- portForServing \"frontend\" -}}")
    .WithServiceBinding(scheme: "http")
    .ExcludeFromManifest();

builder.Build().Run();
