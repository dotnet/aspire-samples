var builder = DistributedApplication.CreateBuilder(args);

// The default value of these parameters is set in the appsettings.json file.
// These values are modeled as parameters so they can be overridden for different environments, etc.
var keycloakRealmName = builder.AddParameter("keycloak-realm");
var keycloakRealmDisplayName = builder.AddParameter("keycloak-realm-display");
var webBlazorSsrClientId = builder.AddParameter("web-blazorssr-client-id");
var webBlazorSsrClientName = builder.AddParameter("web-blazorssr-client-name");
var apiWeatherClientId = builder.AddParameter("api-weather-client-id");
var apiWeatherClientName = builder.AddParameter("api-weather-client-name");

// When running during local development the generated client secrets are stored in user secrets.
var webBlazorSSRClientSecret = builder.AddParameter("web-blazorssr-client-secret", secret: true)
    .WithGeneratedDefault(new() { MinLength = 32, Special = false });
var apiWeatherClientSecret = builder.AddParameter("api-weather-client-secret", secret: true)
    .WithGeneratedDefault(new() { MinLength = 32, Special = false });

var keycloak = builder.AddKeycloak("keycloak")
    .WithImageTag("25.0")
    .WithDataVolume()
    .RunWithHttpsDevCertificate();

var apiWeather = builder.AddProject<Projects.Keycloak_Api_Weather>("api-weather")
    .WithReference(keycloak)
    .WithEnvironment("Authentication__Keycloak__Realm", keycloakRealmName)
    .WithEnvironment("Authentication__Schemes__Bearer__ValidAudience", apiWeatherClientId);

var webBlazorSsr = builder.AddProject<Projects.Keycloak_Web_BlazorSSR>("web-blazorssr")
    .WithExternalHttpEndpoints()
    .WithReference(apiWeather)
    .WithReference(keycloak)
    .WithEnvironment("Authentication__Keycloak__Realm", keycloakRealmName)
    .WithEnvironment("Authentication__Schemes__OpenIdConnect__ClientId", webBlazorSsrClientId)
    .WithEnvironment("Authentication__Schemes__OpenIdConnect__ClientSecret", webBlazorSSRClientSecret);

// Import the sample realm & inject the the values the realm import file requires via environment variables.
keycloak.WithSampleRealmImport(keycloakRealmName, keycloakRealmDisplayName, [
    new("WEB_BLAZORSSR", webBlazorSsrClientId, webBlazorSsrClientName, webBlazorSSRClientSecret, webBlazorSsr),
    new("API_WEATHER", apiWeatherClientId, apiWeatherClientName, apiWeatherClientSecret, apiWeather)
]);

builder.Build().Run();
