var builder = DistributedApplication.CreateBuilder(args);

var rabbitMQ = builder.AddRabbitMQ("messaging").WithManagementPlugin();

builder.AddProject<Projects.AspireWithRabbitMQ_Sender>("sender")
.WithReference(rabbitMQ);

builder.AddProject<Projects.AspireWithRabbitMQ_Receiver>("receiver")
.WithReference(rabbitMQ);

builder.Build().Run();
