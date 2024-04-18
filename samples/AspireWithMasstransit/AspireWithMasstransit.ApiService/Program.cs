using System.Reflection;
using AspireWithMasstransit.ApiService;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("messaging");
        
builder.Services.AddMassTransit(s =>
{
    s.AddConsumers(Assembly.GetExecutingAssembly());
    s.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(connectionString!));
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
