var builder = DistributedApplication.CreateBuilder(args);

// The default value of these parameters is set in the appsettings.json file.
var keycloakRealmName = builder.AddParameter("keycloak-realm");
var keycloakRealmDisplayName = builder.AddParameter("keycloak-realm-display");
var webBlazorSsrClientId = builder.AddParameter("web-blazorssr-client-id");
var webBlazorSsrClientName = builder.AddParameter("web-blazorssr-client-name");
var apiWeatherClientId = builder.AddParameter("api-weather-client-id");
var apiWeatherClientName = builder.AddParameter("api-weather-client-name");

// When running during local development the generated client secret is stored in user secrets.
var webBlazorSSRClientSecret = builder.AddResource(
    ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, "web-blazorssr-client-secret"));
var apiWeatherClientSecret = builder.AddResource(
    ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, "api-weather-client-secret"));

var keycloak = builder.AddKeycloak("keycloak")
    .WithImageTag("25.0")
    .WithDataVolume()
    .WithHttpsDevCertificate()
    // Import the realms & inject the the values the realms import file requires via environment variables.
    .WithRealmImport("realms", isReadOnly: true)
    .WithEnvironment("REALM_NAME", keycloakRealmName)
    .WithEnvironment("REALM_DISPLAY_NAME", keycloakRealmDisplayName)
    .WithEnvironment("CLIENT_WEB_BLAZORSSR_ID", webBlazorSsrClientId)
    .WithEnvironment("CLIENT_WEB_BLAZORSSR_NAME", webBlazorSsrClientName)
    .WithEnvironment("CLIENT_WEB_BLAZORSSR_SECRET", webBlazorSSRClientSecret)
    .WithEnvironment("CLIENT_API_WEATHER_ID", apiWeatherClientId)
    .WithEnvironment("CLIENT_API_WEATHER_NAME", apiWeatherClientName)
    .WithEnvironment("CLIENT_API_WEATHER_SECRET", apiWeatherClientSecret)
    ;

var apiWeather = builder.AddProject<Projects.Keycloak_ApiService>("api-weather")
    .WithReference(keycloak)
    .WithEnvironment("Authentication__Keycloak__Realm", keycloakRealmName)
    .WithEnvironment("Authentication__Schemes__Bearer__ValidAudience", apiWeatherClientId)
    //.WithEnvironment("Authentication__Schemes__OpenIdConnect__ClientSecret", apiWeatherClientSecret)
    ;

var webBlazorSsr = builder.AddProject<Projects.Keycloak_Web_BlazorSSR>("web-blazorssr")
    .WithExternalHttpEndpoints()
    .WithReference(apiWeather)
    .WithReference(keycloak)
    .WithEnvironment("Authentication__Keycloak__Realm", keycloakRealmName)
    .WithEnvironment("Authentication__Schemes__OpenIdConnect__ClientId", webBlazorSsrClientId)
    .WithEnvironment("Authentication__Schemes__OpenIdConnect__ClientSecret", webBlazorSSRClientSecret)
    //.WithEnvironment("idpClientId", webBlazorSsrClientId)
    //.WithEnvironment("idpClientSecret", "67TXVfKutEcIOVtKTmiz201RntcFZgwK")
    ;

// Inject the URLs of our apps into the Keycloak instance so they're picked up by the realms import file.
keycloak
    .WithEnvironment(context =>
    {
        var webBlazorSsrEndpoint = webBlazorSsr.GetEndpoint("https");
        var apiWeatherEndpoint = apiWeather.GetEndpoint("https");

        // Ensure the correct URLs are used depending on the context, e.g. container reference vs. external HTTP endpoint, publish mode vs. run mode.
        context.EnvironmentVariables["CLIENT_WEB_BLAZORSSR_URL"] = context.ExecutionContext.IsPublishMode ? webBlazorSsrEndpoint : webBlazorSsrEndpoint.Url;
        context.EnvironmentVariables["CLIENT_WEB_BLAZORSSR_URL_CONTAINERHOST"] = webBlazorSsrEndpoint;

        context.EnvironmentVariables["CLIENT_API_WEATHER_URL"] = context.ExecutionContext.IsPublishMode ? apiWeatherEndpoint : apiWeatherEndpoint.Url;
        context.EnvironmentVariables["CLIENT_API_WEATHER_URL_CONTAINERHOST"] = apiWeatherEndpoint;
    });

builder.Build().Run();
