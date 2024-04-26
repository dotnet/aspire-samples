using HealthChecksUI.Web;
using HealthChecksUI.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiServiceUri = new Uri("https+http://apiservice");
builder.Services.AddHttpClient<WeatherApiClient>(client => client.BaseAddress = apiServiceUri);

// Add a health-check for the weather API backend
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri(apiServiceUri, "/health"), name: "apiservice");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();

app.UseAntiforgery();

app.UseRequestTimeouts();
app.UseOutputCache();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
