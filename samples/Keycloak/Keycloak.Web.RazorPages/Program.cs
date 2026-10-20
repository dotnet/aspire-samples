using System.Security.Claims;
using Keycloak;
using Keycloak.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddOutputCache();

// Add Keycloak URLs service.
builder.Services.AddSingleton<KeycloakUrls>();

// Add the weather API client.
builder.Services.AddWeatherApiClient(new("https+http://api-weather"), "keycloak");

// Add authentication services (to authenticate end users)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    // Setting the DefaultChallengeScheme to OpenIdConnectDefaults.AuthenticationScheme will automatically redirect users
    // to the Keycloak login page when an authentication challenge occurs.
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
    .AddCookie()
    .AddKeycloakOpenIdConnect("keycloak", builder.Configuration.GetRequiredValue("Authentication:Keycloak:Realm"), oidc =>
    {
        // Most properties including ClientId and ClientSecret are set via configuration from the appsettings.json file.
        oidc.TokenValidationParameters.NameClaimType = KeycloakClaimTypes.PreferredUsername; // Keycloak uses "preferred_username" as the default name claim type.
    });

// Add authorization services
builder.Services.AddAuthorizationBuilder()
    .AddDefaultPolicy("authenticated-users", policy =>
        policy
            .RequireAuthenticatedUser()
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseOutputCache();

app.MapRazorPages();

app.MapDefaultEndpoints();

app.Run();
