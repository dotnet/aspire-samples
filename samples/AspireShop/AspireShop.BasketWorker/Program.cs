using AspireShop.BasketWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("basketcache");
builder.AddRabbitMQClient(connectionName: "messaging");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
