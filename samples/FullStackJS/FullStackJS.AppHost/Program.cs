using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddNpmApp("api", "../api")
                 .WithNpmPackageInstallation()
                 .WithExternalHttpEndpoints()
                 .PublishAsDockerFile();

_ = builder.Environment.IsDevelopment()
    ? api.WithHttpEndpoint(env: "PORT")
    : api.WithHttpsEndpoint(env: "PORT");

builder.AddNpmApp("app", "../app")
       .WithNpmPackageInstallation()
       .WithReference(api)
       .WaitFor(api)
       .WithEnvironment("BROWSER", "none")
       .WithHttpEndpoint(env: "PORT")
       .WithExternalHttpEndpoints()
       .PublishAsDockerFile();

builder.Build().Run();
