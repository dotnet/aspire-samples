using AspireShop.BasketBus;
using AspireShop.BasketWorker;
using AspireShop.Chaos;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("basketcache");
builder.AddRabbitMQClient(connectionName: "messaging");

builder.Services.AddSingleton<IBus, BasicBus>();

builder.Services.AddSingleton<ChaosProvider>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
