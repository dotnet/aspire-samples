using Microsoft.Extensions.Diagnostics.HealthChecks;
using eShopLite.Frontend.Components;
using eShopLite.Frontend.Services;
using eShopLite.GrpcBasket;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpForwarderWithServiceDiscovery();

builder.Services.AddHttpServiceReference<CatalogServiceClient>("http://catalogservice", healthRelativePath: "readiness");

builder.Services.AddSingleton<BasketServiceClient>()
    .AddGrpcServiceReference<Basket.BasketClient>("http://basketservice", failureStatus: HealthStatus.Degraded);

builder.Services.AddRazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseAntiforgery();

app.UseStaticFiles();

app.MapRazorComponents<App>();

app.MapForwarder("/catalog/images/{id}", "http://catalogservice", "/api/v1/catalog/items/{id}/image");

app.MapDefaultEndpoints();

app.Run();
