#pragma warning disable ASPIRECERTIFICATES001

var builder = DistributedApplication.CreateBuilder(args);

// The default value of these parameters is set in the appsettings.json file.
// These values are modeled as parameters so they can be overridden for different environments, etc.
var keycloakRealmName = builder.AddParameter("keycloak-realm");
var keycloakRealmDisplayName = builder.AddParameter("keycloak-realm-display");
var apiWeatherClientId = builder.AddParameter("api-weather-client-id");
var apiWeatherClientName = builder.AddParameter("api-weather-client-name");
var webBlazorSsrClientId = builder.AddParameter("web-blazorssr-client-id");
var webBlazorSsrClientName = builder.AddParameter("web-blazorssr-client-name");
var webRazorPagesClientId = builder.AddParameter("web-razorpages-client-id");
var webRazorPagesClientName = builder.AddParameter("web-razorpages-client-name");

// When running during local development the generated client secrets are stored in user secrets.
var apiWeatherClientSecret = builder.AddParameter("api-weather-client-secret", secret: true)
    .WithGeneratedDefault(new() { MinLength = 32, Special = false });
var webBlazorSSRClientSecret = builder.AddParameter("web-blazorssr-client-secret", secret: true)
    .WithGeneratedDefault(new() { MinLength = 32, Special = false });
var webRazorPagesClientSecret = builder.AddParameter("web-razorpages-client-secret", secret: true)
    .WithGeneratedDefault(new() { MinLength = 32, Special = false });

var keycloak = builder.AddKeycloak("keycloak")
    .WithDataVolume()
    .WithHttpsDeveloperCertificate();

if (builder.ExecutionContext.IsRunMode)
{
    keycloak
        .WithEnvironment("KC_HOSTNAME", "localhost")
        // Without disabling HTTP/2 you can hit HTTP 431 Header too large errors in Keycloak.
        // Related issues:
        // https://github.com/keycloak/keycloak/discussions/10236
        // https://github.com/keycloak/keycloak/issues/13933
        // https://github.com/quarkusio/quarkus/issues/33692
        .WithEnvironment("QUARKUS_HTTP_HTTP2", "false");
}

var apiWeather = builder.AddProject<Projects.Keycloak_Api_Weather>("api-weather")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__Keycloak__Realm", keycloakRealmName)
    .WithEnvironment("Authentication__Schemes__Bearer__ValidAudience", apiWeatherClientId);

var webBlazorSsr = builder.AddProject<Projects.Keycloak_Web_BlazorSSR>("web-blazorssr")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(apiWeather)
    .WaitFor(apiWeather)
    .WithEnvironment("Authentication__Keycloak__Realm", keycloakRealmName)
    .WithEnvironment("Authentication__Schemes__OpenIdConnect__ClientId", webBlazorSsrClientId)
    .WithEnvironment("Authentication__Schemes__OpenIdConnect__ClientSecret", webBlazorSSRClientSecret);

var webRazorPages = builder.AddProject<Projects.Keycloak_Web_RazorPages>("web-razorpages")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(apiWeather)
    .WaitFor(apiWeather)
    .WithEnvironment("Authentication__Keycloak__Realm", keycloakRealmName)
    .WithEnvironment("Authentication__Schemes__OpenIdConnect__ClientId", webRazorPagesClientId)
    .WithEnvironment("Authentication__Schemes__OpenIdConnect__ClientSecret", webRazorPagesClientSecret);

// Import the sample realm & inject the values the realm import file requires via environment variables.
keycloak.WithSampleRealmImport(keycloakRealmName, keycloakRealmDisplayName, [
    new("API_WEATHER", apiWeatherClientId, apiWeatherClientName, apiWeatherClientSecret, apiWeather),
    new("WEB_BLAZORSSR", webBlazorSsrClientId, webBlazorSsrClientName, webBlazorSSRClientSecret, webBlazorSsr),
    new("WEB_RAZORPAGES", webRazorPagesClientId, webRazorPagesClientName, webRazorPagesClientSecret, webRazorPages),
]);

builder.Build().Run();
