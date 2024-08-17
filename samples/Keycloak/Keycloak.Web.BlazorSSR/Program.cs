using System.Security.Claims;
using Keycloak;
using Keycloak.Web.BlazorSSR;
using Keycloak.Web.BlazorSSR.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

var idpClientId = builder.Configuration.GetRequiredValue("idpClientId");
var idpClientSecret = builder.Configuration.GetRequiredValue("idpClientSecret");
var idpRealmName = builder.Configuration.GetRequiredValue("idpRealmName");
var idpAuthority = $"http://idp/realms/{idpRealmName}";

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

// Add authentication services (to authenticate this app to downstream APIs)
builder.Services.AddScoped<ForceHttpMessageHandler>();
builder.Services.AddHttpClient("Msal", client => client.BaseAddress = new(idpAuthority))
    .AddHttpMessageHandler<ForceHttpMessageHandler>()
    .AddServiceDiscovery();
builder.Services.AddSingleton<IMsalHttpClientFactory, MsalHttpClientFactory>();
builder.Services.AddSingleton(sp =>
    ConfidentialClientApplicationBuilder.Create(idpClientId)
        .WithExperimentalFeatures(true)
        .WithClientSecret(idpClientSecret)
        .WithHttpClientFactory(sp.GetRequiredService<IMsalHttpClientFactory>())
        // MSAL doesn't allow non-HTTPS authority URLs so we say it's HTTPS here and then force it to HTTP with the ForceHttpMessageHandler
        .WithOidcAuthority($"https://idp/realms/{idpRealmName}")
        .Build()
);

builder.Services.AddScoped<AppAuthenticationMessageHandler>();

builder.Services.AddHttpClient<WeatherApiClient>(client => client.BaseAddress = new("https+http://apiservice"))
    .AddHttpMessageHandler<AppAuthenticationMessageHandler>();

// Add authentication services (to authenticate end users)
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        // Setting the DefaultChallengeScheme to OpenIdConnectDefaults.AuthenticationScheme will automatically redirect users
        // to the Keycloak login page when an authentication challenge occurs.
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    })
    .AddKeycloakOpenIdConnect("idp", idpRealmName, oidc =>
    {
        oidc.ClientId = idpClientId;
        oidc.ClientSecret = idpClientSecret;
        oidc.ResponseType = OpenIdConnectResponseType.Code; // Ensure we're configured to use the authorization code flow
        oidc.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        oidc.Scope.Add(OpenIdConnectScope.OpenIdProfile);
        oidc.Scope.Add(OpenIdConnectScope.Email);
        oidc.Scope.Add(OpenIdConnectScope.Address);
        oidc.SaveTokens = true; // Required to save the id and access tokens returned by Keycloak for later use, including logout.
        oidc.MapInboundClaims = false; // Prevent from mapping "sub" claim to nameidentifier.
        oidc.TokenValidationParameters.NameClaimType = KeycloakClaimTypes.PreferredUsername; // Keycloak uses "preferred_username" as the default name claim type.
    });

// Add authorization services
builder.Services.AddAuthorizationBuilder()
    .AddDefaultPolicy("authenticated-users", policy =>
        policy
            .RequireAuthenticatedUser()
    );

// Add Keycloak URLs service
builder.Services.AddSingleton<KeycloakUrls>();

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
