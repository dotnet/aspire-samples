import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

builder.addDockerComposeEnvironment("compose");

const cache = builder.addRedis("cache");

const apiService = builder.addProject("apiservice", "../HealthChecksUI.ApiService/HealthChecksUI.ApiService.csproj", "https")
    .withHttpHealthCheck("/health");

const webFrontend = builder.addProject("webfrontend", "../HealthChecksUI.Web/HealthChecksUI.Web.csproj", "https")
    .withReference(cache)
    .waitFor(cache)
    .withReference(apiService)
    .waitFor(apiService)
    .withHttpHealthCheck("/health")
    .withExternalHttpEndpoints();

// Note: AddHealthChecksUI is not yet available in the TypeScript SDK.
// See the cs/ directory for the full implementation with HealthChecksUI dashboard.

await builder.build().run();
