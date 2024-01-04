var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ClientAppsServiceDiscovery_ApiService>("apiservice");

builder.AddProject<Projects.ClientAppsServiceDiscovery_Web>("webfrontend")
    .WithReference(apiService);

builder.AddProject("winformsclient", "../ClientAppsServiceDiscovery.WinForms/ClientAppsServiceDiscovery.WinForms.csproj")
    .WithReference(apiService);

builder.Build().Run();
