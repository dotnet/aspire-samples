using System.Globalization;
using Aspire.Hosting;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedisContainer("cache");

var weatherapi = builder.AddProject<Projects.AspireWithNode_AspNetCoreApi>("weatherapi");

var frontendPort = 8080;
var frontend = builder.AddExecutable("frontend", "node", "../NodeFrontend", "app.js")
    .WithReference(weatherapi)
    .WithReference(cache)
    .WithOtlpExporter()
    .WithEnvironment("NODE_ENV", builder.Environment.IsDevelopment() ? "development" : "production")
    .WithEnvironment("PORT", frontendPort.ToString(CultureInfo.InvariantCulture))
    .WithServiceBinding(frontendPort, "http");

builder.Build().Run();
