var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos");

var apiService = builder.AddProject<Projects.AspireWithCosmos_ApiService>("apiservice").WithReference(cosmos);

builder.AddProject<Projects.AspireWithCosmos_Web>("webfrontend")
       .WithExternalHttpEndpoints()
       .WithReference(apiService);

builder.Build().Run();
