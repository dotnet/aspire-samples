using Microsoft.EntityFrameworkCore;
using AspireShop.CatalogDb;
using AspireShop.CatalogDbManager;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb", null,
    optionsBuilder => optionsBuilder.UseNpgsql(npgsqlBuilder =>
        npgsqlBuilder.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)));

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(CatalogDbInitializer.ActivitySourceName));

builder.Services.AddSingleton<CatalogDbInitializer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CatalogDbInitializer>());
builder.Services.AddHealthChecks()
    .AddCheck<CatalogDbInitializerHealthCheck>("DbInitializer", null);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapPost("/reset-db", async (CatalogDbContext dbContext, CatalogDbInitializer dbInitializer, CancellationToken cancellationToken) =>
    {
        // Delete and recreate the database. This is useful for development scenarios to reset the database to its initial state.
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbInitializer.InitializeDatabaseAsync(dbContext, cancellationToken);
    });
}

app.MapDefaultEndpoints();

await app.RunAsync();
