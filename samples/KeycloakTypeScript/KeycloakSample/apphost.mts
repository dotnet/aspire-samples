import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

// Parameters for the Keycloak realm configuration.
const keycloakRealmName = builder.addParameter("keycloak-realm", { value: "aspirekeycloaksample" });
const keycloakRealmDisplayName = builder.addParameter("keycloak-realm-display", { value: "Aspire Keycloak Sample" });
const apiWeatherClientId = builder.addParameter("api-weather-client-id", { value: "keycloak.api.weather" });
const apiWeatherClientName = builder.addParameter("api-weather-client-name", { value: "Weather API" });
const webFrontendClientId = builder.addParameter("web-frontend-client-id", { value: "keycloak.web.frontend" });
const webFrontendClientName = builder.addParameter("web-frontend-client-name", { value: "React Frontend" });

// Generated secret for the API Weather client (service account).
const apiWeatherClientSecret = builder.addParameterWithGeneratedValue(
    "api-weather-client-secret",
    { minLength: 32, special: false },
    { secret: true }
);

// Keycloak identity provider.
const keycloak = builder.addKeycloak("keycloak")
    .withDataVolume()
    .withHttpsDeveloperCertificate()
    .withRealmImport("realms")
    .withEnvironment("REALM_NAME", keycloakRealmName)
    .withEnvironment("REALM_DISPLAY_NAME", keycloakRealmDisplayName)
    .withEnvironment("REALM_HSTS", "")
    .withEnvironment("CLIENT_API_WEATHER_ID", apiWeatherClientId)
    .withEnvironment("CLIENT_API_WEATHER_NAME", apiWeatherClientName)
    .withEnvironment("CLIENT_API_WEATHER_SECRET", apiWeatherClientSecret)
    .withEnvironment("CLIENT_WEB_FRONTEND_ID", webFrontendClientId)
    .withEnvironment("CLIENT_WEB_FRONTEND_NAME", webFrontendClientName);

// Apply run-mode-only settings.
const isRunMode = await builder.executionContext().isRunMode();
if (isRunMode) {
    await keycloak
        .withEnvironment("KC_HOSTNAME", "localhost")
        // Without disabling HTTP/2 you can hit HTTP 431 Header too large errors in Keycloak.
        .withEnvironment("QUARKUS_HTTP_HTTP2", "false");
}

// Python Weather API with JWT Bearer authentication.
const app = await builder
    .addUvicornApp("app", "./app", "main:app")
    .withUv()
    .withExternalHttpEndpoints()
    .withHttpHealthCheck({ path: "/health" })
    .withReference(keycloak)
    .waitFor(keycloak)
    .withEnvironment("KEYCLOAK_REALM", keycloakRealmName)
    .withEnvironment("KEYCLOAK_AUDIENCE", apiWeatherClientId);

// React frontend with OIDC authentication.
const frontend = await builder
    .addViteApp("frontend", "./frontend")
    .withExternalHttpEndpoints()
    .withReference(app)
    .withReference(keycloak)
    .waitFor(app)
    .withEnvironment("KEYCLOAK_REALM", keycloakRealmName)
    .withEnvironment("KEYCLOAK_CLIENT_ID", webFrontendClientId);

// Inject the client URLs into Keycloak for realm import using a callback.
// The callback runs during env resolution when endpoints are allocated, giving us the
// host-accessible URL (for redirect_uri matching) vs. the container-internal reference.
await keycloak.withEnvironmentCallback(async (ctx) => {
    const env = ctx.environment();
    const appEndpoint = app.getEndpoint("http");
    const frontendEndpoint = frontend.getEndpoint("http");

    // Host-accessible URLs (used by browser for redirect_uri matching)
    const appUrl = await appEndpoint.url();
    const frontendUrl = await frontendEndpoint.url();
    await env.set("CLIENT_API_WEATHER_URL", appUrl);
    await env.set("CLIENT_WEB_FRONTEND_URL", frontendUrl);

    // Container-internal references (used by Keycloak for backend calls)
    await env.set("CLIENT_API_WEATHER_URL_CONTAINERHOST", appEndpoint);
    await env.set("CLIENT_WEB_FRONTEND_URL_CONTAINERHOST", frontendEndpoint);
});

// Bundle the frontend build output into the API container for publish/deploy.
await app.publishWithContainerFiles(frontend, "./static");

await builder.build().run();
