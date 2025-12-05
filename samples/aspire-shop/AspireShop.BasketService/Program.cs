using System.Diagnostics;
using AspireShop.BasketService;
using AspireShop.BasketService.Repositories;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("basketcache");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddRedisInstrumentation(options =>
            {
                // Don't trace commands related to the health check to avoid filling the dashboard with noise
                options.Enrich = (activity, command) =>
                {
                    if (command.ProfiledCommand.Command == "PING")
                    {
                        activity.IsAllDataRequested = false;
                        activity.ActivityTraceFlags = ActivityTraceFlags.None;
                    }
                };
            });
    });

builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();
builder.Services.AddTransient<IBasketRepository, RedisBasketRepository>();

var app = builder.Build();

app.MapGrpcService<BasketService>();

app.MapGrpcHealthChecksService();

app.MapDefaultEndpoints();

app.Run();
