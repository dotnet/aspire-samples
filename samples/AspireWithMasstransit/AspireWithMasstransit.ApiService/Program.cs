using System.Reflection;
using AspireWithMasstransit.ApiService;
using AspireWithMasstransit.ServiceDefaults;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("messaging")
                       ?? throw new InvalidOperationException("Connection string for RabbitMQ instance 'messaging' was not found.");        

builder.Services.AddMassTransit(s =>
{
    s.AddConsumers(typeof(Program).Assembly);
    s.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(connectionString));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapGet("/", async (IPublishEndpoint publishEndpoint) =>
{
    await publishEndpoint.Publish(new HelloAspireEvent("Hello, .NET!"));
    await publishEndpoint.Publish(new HelloAspireEvent("Hello, Aspire!"));
    
    return Results.Ok("ok");
});

app.MapDefaultEndpoints();

app.Run();
