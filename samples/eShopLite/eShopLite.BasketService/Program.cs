using eShopLite.BasketService;
using eShopLite.BasketService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedis("basketcache");

builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();
builder.Services.AddTransient<IBasketRepository, RedisBasketRepository>();

var app = builder.Build();

app.MapGrpcService<BasketService>();

app.MapGrpcHealthChecksService();

app.MapDefaultEndpoints();

app.Run();
