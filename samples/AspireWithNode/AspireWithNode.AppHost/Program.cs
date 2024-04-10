using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var weatherapi = builder.AddProject<Projects.AspireWithNode_AspNetCoreApi>("weatherapi");

var frontend = builder.AddNpmApp("frontend", "../NodeFrontend", "watch")
    .WithReference(weatherapi)
    .WithReference(cache)
    .WithHttpEndpoint(targetPort: 3000, env: "PORT")
    .PublishAsDockerFile();

if (builder.Environment.IsDevelopment() && builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "https")
{
    // Disable TLS certificate validation in development, see https://github.com/dotnet/aspire/issues/3324 for more details.
    frontend.WithEnvironment("NODE_TLS_REJECT_UNAUTHORIZED", "0");
}

builder.Build().Run();
