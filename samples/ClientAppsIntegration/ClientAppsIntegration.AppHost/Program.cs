var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ClientAppsIntegration_ApiService>("apiservice");

if (OperatingSystem.IsWindows())
{
    // Register the client apps by project path as they target a TFM incompatible with the AppHost so can't be added as
    // regular project references (see the AppHost.csproj file for additional metadata added to the ProjectReference to
    // coordinate a build dependency though).
    builder.AddProject<Projects.ClientAppsIntegration_WinForms>("winformsclient")
        .WithReference(apiService)
        .ExcludeFromManifest();

    builder.AddProject<Projects.ClientAppsIntegration_WPF>("wpfclient")
        .WithReference(apiService)
        .ExcludeFromManifest();
}

builder.Build().Run();
