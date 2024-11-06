using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var goVersion = builder.AddParameter("goversion", "1.22", publishValueAsDefault: true);

var ginapp = builder.AddDockerfile("ginapp", "../ginapp")
    .WithBuildArg("GO_VERSION", goVersion)
    .WithHttpEndpoint(targetPort: 5555, env: "PORT")
    .WithExternalHttpEndpoints();

ginapp.WithEnvironment("TRUSTED_PROXIES", $"{ginapp.GetEndpoint("http").Property(EndpointProperty.Host)}");

if (builder.ExecutionContext.IsRunMode || builder.Environment.IsProduction())
{
    ginapp.WithEnvironment("GIN_MODE", "release");
}

builder.Build().Run();
