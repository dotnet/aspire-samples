using Listly.Database;
using Listly.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ListlyDbContext>("listly");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(ListlyDbInitializer.ActivitySourceName));

builder.Services.AddSingleton<ListlyDbInitializer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ListlyDbInitializer>());
builder.Services.AddHealthChecks()
    .AddCheck<ListlyDbInitializerHealthCheck>("DbInitializer", null);
    
var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();