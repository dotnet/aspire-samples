var builder = DistributedApplication.CreateBuilder(args);

// The default value of these parameters is set in the appsettings.json file.
var keycloakRealmName = builder.AddParameter("keycloak-realm");
var keycloakRealmDisplayName = builder.AddParameter("keycloak-realm-display");
var webBlazorSsrClientId = builder.AddParameter("web-blazorssr-client-id");
var webBlazorSsrClientName = builder.AddParameter("web-blazorssr-client-name");
var apiWeatherClientId = builder.AddParameter("api-weather-client-id");
var apiWeatherClientName = builder.AddParameter("api-weather-client-name");

// When running during local development the generated client secret is stored in user secrets.
var webBlazorSSRClientSecret = builder.AddClientSecretParameter("web-blazorssr-client-secret");
var apiWeatherClientSecret = builder.AddClientSecretParameter("api-weather-client-secret");

var keycloak = builder.AddKeycloak("keycloak")
    .WithImageTag("25.0")
    .WithDataVolume()
    .RunWithHttpsDevCertificate();

var apiWeather = builder.AddProject<Projects.Keycloak_ApiService>("api-weather")
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
