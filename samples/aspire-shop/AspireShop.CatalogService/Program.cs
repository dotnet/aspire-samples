using AspireShop.CatalogDb;
using AspireShop.CatalogService;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb", null,
    optionsBuilder => optionsBuilder.UseNpgsql(npgsqlBuilder =>
    {
        npgsqlBuilder.ConfigureDataSource(dataSourceBuilder =>
        {
            dataSourceBuilder.ConfigureTracing(options =>
            {
                options.ConfigureCommandFilter(cmd =>
                {
                    // Don't trace commands related to the health check to avoid filling the dashboard with noise
                    if (cmd.CommandText.Contains("SELECT 1"))
                    {
                        return false;
                    }
                    return true;
                });
            });
        });
    }));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler();
}

app.MapCatalogApi();
app.MapDefaultEndpoints();

app.Run();
