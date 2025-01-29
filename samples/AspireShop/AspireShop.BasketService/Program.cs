using AspireShop.BasketBus;
using AspireShop.BasketService;
using AspireShop.BasketService.Repositories;
using AspireShop.Chaos;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("basketcache");
builder.AddRabbitMQClient(connectionName: "messaging");

builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();
builder.Services.AddTransient<IBasketRepository, RedisBasketRepository>();

builder.Services.AddSingleton<IBus, BasicBus>();

builder.Services.AddSingleton<ChaosProvider>();

var app = builder.Build();

app.MapGrpcService<BasketService>();

app.MapGrpcHealthChecksService();

app.MapDefaultEndpoints();

app.Run();
