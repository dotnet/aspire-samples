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
builder.Services.AddHttpClient(MsalHttpClientFactory.HttpClientName, client => client.BaseAddress = new(idpAuthority))
    // We haven't configured Keycloak to support HTTPS so we have to force it to HTTP here & then add
    // AddServiceDiscovery() again *after* we've forced HTTP so that service discovery finds the configured HTTP addresses.
    // We could alternatively try to configure Keycloak to support HTTPS or add a YARP gateway in front of it to handle HTTPS during dev.
    .AddHttpMessageHandler<ForceHttpMessageHandler>() 
    .AddServiceDiscovery();
builder.Services.AddSingleton<IMsalHttpClientFactory, MsalHttpClientFactory>();
builder.Services.AddSingleton(sp =>
    {
        // TODO: Need to investigate using Microsoft.Identity.Web instead of MSAL.NET directly.
        //       Additionally should consider using a distributed cache for the token cache, e.g. Redis, so tokens are cached across multiple
        //       app instances as per documented recommendations at https://learn.microsoft.com/entra/msal/dotnet/how-to/token-cache-serialization?tabs=msal#distributed-caches
        var app = ConfidentialClientApplicationBuilder.Create(idpClientId)
            .WithClientSecret(idpClientSecret)
            // MSAL doesn't allow non-HTTPS authority URLs so we say it's HTTPS here and then force it to HTTP with the ForceHttpMessageHandler
            .WithOidcAuthority(idpAuthority.Replace("http://", "https://"))
            .WithInstanceDiscovery(false)
            // Configure MSAL token caching as per https://learn.microsoft.com/entra/msal/dotnet/how-to/token-cache-serialization?tabs=msal#memory-cache-without-eviction/
            // BUG: I don't think this is working as expected as access tokens are not being cached.
            .WithLegacyCacheCompatibility(false)
            .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
            // Configure MSAL to use IHttpClientFactory via our custom IMsalHttpClientFactory implementation
            .WithHttpClientFactory(sp.GetRequiredService<IMsalHttpClientFactory>())
            .Build();
        return app;
    }
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
