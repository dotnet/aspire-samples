var builder = DistributedApplication.CreateBuilder(args);

var tododb = builder.AddAzureCosmosDB("cosmos").AddDatabase("tododb")
    .RunAsEmulator(); // remove this line should you want to use a live instance during development
                      // or should you not have the Azure Cosmos DB emulator installed.

var apiService = builder.AddProject<Projects.AspireWithCosmos_ApiService>("apiservice")
    .WithReference(tododb);

builder.AddProject<Projects.AspireWithCosmos_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
