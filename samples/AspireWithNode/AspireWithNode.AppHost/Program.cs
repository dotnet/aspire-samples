using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .RunWithHttpsDevCertificate();

var weatherapi = builder.AddProject<Projects.AspireWithNode_AspNetCoreApi>("weatherapi");

var frontend = builder.AddNpmApp("frontend", "../NodeFrontend", "watch")
    .WithReference(weatherapi)
    .WaitFor(weatherapi)
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"] ??
                    builder.Configuration["AppHost:DefaultLaunchProfileName"]; // work around https://github.com/dotnet/aspire/issues/5093

if (builder.Environment.IsDevelopment() && launchProfile == "https")
{
    frontend.RunWithHttpsDevCertificate(CertificateFileFormat.PfxWithPassword, "HTTPS_CERT_PFX_FILE", "HTTPS_CERT_PASSWORD");
}

builder.Build().Run();
