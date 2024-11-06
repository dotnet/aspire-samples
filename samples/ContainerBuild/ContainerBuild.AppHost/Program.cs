using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// BUG: azd doesn't properly support parameters with default values yet
//var goVersion = builder.AddParameter("goversion", "1.22", publishValueAsDefault: true);
// Workaround: Default value used when running locally comes from appsettings.Development.json.
//             A value must be provided when running azd up (use '1.22').
var goVersion = builder.AddParameter("goversion");

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
