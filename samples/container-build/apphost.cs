#:sdk Aspire.AppHost.Sdk@13.0.0

using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var goVersion = builder.AddParameter("goversion", "1.25.4", publishValueAsDefault: true);

IResourceBuilder<ContainerResource> ginapp;

if (builder.ExecutionContext.IsPublishMode)
{
    // Production build: multi-stage Dockerfile for optimized image
    ginapp = builder.AddDockerfile("ginapp", "./ginapp")
        .WithBuildArg("GO_VERSION", goVersion);
}
else
{
    // Development build: use Air for hot reload with bind mount
    ginapp = builder.AddDockerfile("ginapp", "./ginapp", "Dockerfile.dev")
        .WithBuildArg("GO_VERSION", goVersion)
        .WithBindMount("./ginapp", "/app");
}

ginapp
    .WithHttpEndpoint(targetPort: 5555, env: "PORT")
    .WithHttpHealthCheck("/")
    .WithExternalHttpEndpoints()
    .WithOtlpExporter()
    .WithDeveloperCertificateTrust(trust: true);

if (builder.ExecutionContext.IsPublishMode)
{
    ginapp
        .WithEnvironment("GIN_MODE", "release")
        // Trust all proxies when running behind a reverse proxy. If deploying to an environment
        // without a reverse proxy that ensures X-Forwarded-* headers are not forwarded from clients,
        // this should be removed.
        .WithEnvironment("TRUSTED_PROXIES", "all");
}

builder.Build().Run();
