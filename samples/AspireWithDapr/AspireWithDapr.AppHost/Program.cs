var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AspireWithDapr_ApiService>("apiservice")
       .WithDaprSidecar("api");

builder.AddProject<Projects.AspireWithDapr_Web>("webfrontend")
       .WithDaprSidecar("web");

builder.Build().Run();
