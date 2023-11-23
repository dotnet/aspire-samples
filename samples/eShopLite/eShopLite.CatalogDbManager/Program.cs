using eShopLite.CatalogDb;
using eShopLite.CatalogDbManager;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb", null,
    c => c.UseNpgsql(a => a.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)));

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(CatalogDbInitializer.ActivitySourceName));

builder.Services.AddSingleton<CatalogDbInitializer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CatalogDbInitializer>());
builder.Services.AddHealthChecks()
    .AddCheck<CatalogDbInitializerHealthCheck>("DbInitializer", null);

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();
