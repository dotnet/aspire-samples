import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

await builder.addDockerComposeEnvironment("compose");

const cache = await builder.addRedis("cache");

const apiService = await builder.addProject("apiservice", "../HealthChecksUI.ApiService/HealthChecksUI.ApiService.csproj", "https")
    .withHttpHealthCheck({
        path: "/health"
    });

const webFrontend = await builder.addProject("webfrontend", "../HealthChecksUI.Web/HealthChecksUI.Web.csproj", "https")
    .withReference(cache)
    .waitFor(cache)
    .withServiceReference(apiService)
    .waitFor(apiService)
    .withHttpHealthCheck({
        path: "/health"
    })
    .withExternalHttpEndpoints();

// Note: AddHealthChecksUI is not yet available in the TypeScript SDK.
// See the cs/ directory for the full implementation with HealthChecksUI dashboard.

await builder.build().run();
