using AspireWithMassTransit.ApiService;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddMassTransit(s =>
{
    s.AddConsumers(typeof(Program).Assembly);
    s.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var host = configuration.GetConnectionString("messaging");

        cfg.Host(host);
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

