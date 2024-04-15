using System.Reflection;
using AspireWithMasstransit.ApiService;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("messaging");
        
builder.Services.AddMassTransit(s =>
{
    s.AddConsumers(Assembly.GetExecutingAssembly());
    s.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(connectionString!), "/");
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();


app.MapGet("/", async (IPublishEndpoint publishEndpoint) =>
{
    await publishEndpoint.Publish(new HelloAspireEvent("Hello, .NET!"));
    await publishEndpoint.Publish(new HelloAspireEvent("Hello, Aspire!"));
    
    return Results.Ok("ok");
});

app.MapDefaultEndpoints();

app.Run();
