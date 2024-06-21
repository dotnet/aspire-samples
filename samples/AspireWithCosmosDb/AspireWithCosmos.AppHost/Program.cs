using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var tododb = builder.AddAzureCosmosDB("cosmos").AddDatabase("tododb")
    // Remove the RunAsEmulator() line should you want to use a live instance during development
    .RunAsEmulator(); 

var apiService = builder.AddProject<Projects.AspireWithCosmos_ApiService>("apiservice")
    .WithReference(tododb);

builder.AddProject<Projects.AspireWithCosmos_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
