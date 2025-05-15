var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ClientAppsIntegration_ApiService>("apiservice");

if (OperatingSystem.IsWindows())
{
    builder.AddProject<Projects.ClientAppsIntegration_WinForms>("winformsclient")
        .WithReference(apiService)
        .WaitFor(apiService)
        .WithExplicitStart()
        .ExcludeFromManifest();

    builder.AddProject<Projects.ClientAppsIntegration_WPF>("wpfclient")
        .WithReference(apiService)
        .WaitFor(apiService)
        .WithExplicitStart()
        .ExcludeFromManifest();
}

builder.Build().Run();
