using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

var idpRealmName = builder.Configuration.GetRequiredValue("idpRealmName");
var idpClientName = "keycloak.apiservice";

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddKeycloakJwtBearer("idp", idpRealmName, jwtBearer =>
    {
        jwtBearer.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        jwtBearer.Audience = idpClientName;
    });

builder.Services.AddAuthorizationBuilder()
    .AddDefaultPolicy("api-callers", policy =>
        policy
            .RequireAuthenticatedUser()
            //.RequireRole("api-callers")
            .RequireAssertion(context =>
            {
                var resourceAcecss = context.User.FindFirst("resource_access");
                if (resourceAcecss is { } resourceAccessClaim && string.Equals(resourceAccessClaim.ValueType, "JSON", StringComparison.OrdinalIgnoreCase))
                {
                    // Payload example: {"resource-name":{"roles":["role-name"]},"account":{"roles":["manage-account","manage-account-links","view-profile"]}}
                    var resourceAccessJson = JsonNode.Parse(resourceAccessClaim.Value);
                    if (resourceAccessJson is { } && resourceAccessJson[idpClientName] is JsonObject resourceNode
                        && resourceNode["roles"] is JsonArray resourceRoles)
                    {
                        var hasRole = resourceRoles.GetValues<string>().Contains("api-callers");
                        if (hasRole)
                        {
                            foreach (var req in context.Requirements)
                            {
                                context.Succeed(req);
                            }
                            return true;
                        }
                    }
                }
                context.Fail();
                return false;
            })
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
