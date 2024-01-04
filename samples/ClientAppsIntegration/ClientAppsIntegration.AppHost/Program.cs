var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ClientAppsIntegration_ApiService>("apiservice");

builder.AddProject<Projects.ClientAppsIntegration_Web>("webfrontend")
    .WithReference(apiService);

// Register the WinForms client app by project path as it targets a TFM incompatible with the AppHost so it can't be added as a
// regular project reference (see the AppHost.csproj file for additional metadata added to the ProjectReference to coordinate a
// build dependency though).
builder.AddProject("winformsclient", "../ClientAppsIntegration.WinForms/ClientAppsIntegration.WinForms.csproj")
    .WithReference(apiService);

builder.Build().Run();
