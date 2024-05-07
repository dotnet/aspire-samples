using AspireShop.BasketService;
using AspireShop.BasketService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("basketcache");

builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();
builder.Services.AddTransient<IBasketRepository, RedisBasketRepository>();

var app = builder.Build();

app.MapGrpcService<BasketService>();

app.MapGrpcHealthChecksService();

app.MapDefaultEndpoints();

app.Run();
