using System.Security.Claims;
using Keycloak;
using Keycloak.Web;
using Keycloak.Web.BlazorSSR.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

// Add Blazor authentication services
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseOutputCache();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
