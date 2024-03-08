var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireWithSerilog_ApiService>("apiservice");

builder.AddProject<Projects.AspireWithSerilog_Web>("webfrontend")
    .WithReference(apiService);

builder.Build().Run();
