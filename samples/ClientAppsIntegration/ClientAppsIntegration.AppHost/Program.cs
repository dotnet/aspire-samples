var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ClientAppsIntegration_ApiService>("apiservice");

// Register the client apps by project path as they target a TFM incompatible with the AppHost so can't be added as
// regular project references (see the AppHost.csproj file for additional metadata added to the ProjectReference to
// coordinate a build dependency though).
builder.AddProject("winformsclient", "../ClientAppsIntegration.WinForms/ClientAppsIntegration.WinForms.csproj")
    .WithReference(apiService);

builder.AddProject("wpfclient", "../ClientAppsIntegration.WPF/ClientAppsIntegration.WPF.csproj")
    .WithReference(apiService);

builder.Build().Run();
