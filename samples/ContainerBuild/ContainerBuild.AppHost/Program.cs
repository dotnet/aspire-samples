using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var goVersion = builder.AddParameter("goversion", "1.24.2", publishValueAsDefault: true);

var ginapp = builder.AddDockerfile("ginapp", "../ginapp")
    .WithBuildArg("GO_VERSION", goVersion)
    .WithHttpEndpoint(targetPort: 5555, env: "PORT")
    .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsPublishMode || builder.Environment.IsProduction())
{
    ginapp
        .WithEnvironment("GIN_MODE", "release")
        // Trust all proxies when running behind a reverse proxy. If deploying to an environment
        // without a reverse proxy that ensures X-Forwarded-* headers are not forwarded from clients,
        // this should be removed.
        .WithEnvironment("TRUSTED_PROXIES", "all");
}

builder.Build().Run();
