var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging");

var apiService = builder.AddProject<Projects.AspireWithMasstransit_ApiService>("apiservice")
    .WithReference(messaging);

builder.Build().Run();
