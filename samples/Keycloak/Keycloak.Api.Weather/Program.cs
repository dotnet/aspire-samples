using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    // Settings for the JwtBearerOptions are bound from configuration.
    .AddKeycloakJwtBearer("keycloak", builder.Configuration.GetRequiredValue("Authentication:Keycloak:Realm"));

builder.Services.AddKeycloakClaimsTransformation();
builder.Services.AddAuthorizationBuilder()
    // The default authorization policy will apply to all endpoints that are marked as requiring authorization
    // and don't specify a specific policy.
    .AddDefaultPolicy("api-callers", policy =>
        policy
            .RequireAuthenticatedUser()
            .RequireRole("api-callers")
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
    );

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

var apis = app.MapGroup("/api")
    .RequireAuthorization();

apis.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
