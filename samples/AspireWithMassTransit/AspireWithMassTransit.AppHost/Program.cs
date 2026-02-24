var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin()
    .WithDataVolume();

var apiService = builder.AddProject<Projects.AspireWithMassTransit_ApiService>("apiservice")
    .WithReference(messaging);

builder.Build().Run();
