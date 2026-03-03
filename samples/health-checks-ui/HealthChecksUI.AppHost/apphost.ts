import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

// POLYGLOT GAP: AddDockerComposeEnvironment("compose") — Docker Compose integration is not available in the TypeScript polyglot SDK.
// builder.AddDockerComposeEnvironment("compose");

const cache = builder.addRedis("cache");

// POLYGLOT GAP: AddProject<Projects.HealthChecksUI_ApiService>("apiservice") — generic type parameter for project reference is not available.
// POLYGLOT GAP: .WithHttpProbe(ProbeType.Liveness, "/alive") — probe configuration is not available.
// POLYGLOT GAP: .WithFriendlyUrls(displayText: "API") — WithFriendlyUrls is a custom extension method not available in the TypeScript SDK.
const apiService = builder.addProject("apiservice")
    .withHttpHealthCheck("/health");

// POLYGLOT GAP: AddProject<Projects.HealthChecksUI_Web>("webfrontend") — generic type parameter for project reference is not available.
// POLYGLOT GAP: .WithHttpProbe(ProbeType.Liveness, "/alive") — probe configuration is not available.
// POLYGLOT GAP: .WithFriendlyUrls("Web Frontend") — WithFriendlyUrls is a custom extension method not available.
const webFrontend = builder.addProject("webfrontend")
    .withReference(cache)
    .waitFor(cache)
    .withReference(apiService)
    .waitFor(apiService)
    .withHttpHealthCheck("/health")
    .withExternalHttpEndpoints();

// POLYGLOT GAP: AddHealthChecksUI("healthchecksui") — HealthChecks UI integration is not available in the TypeScript polyglot SDK.
// POLYGLOT GAP: .WithReference(apiService).WithReference(webFrontend) — references for health checks UI.
// POLYGLOT GAP: .WithFriendlyUrls("HealthChecksUI Dashboard", "http") — custom extension method not available.
// POLYGLOT GAP: .WithHttpProbe(ProbeType.Liveness, "/") — probe configuration is not available.
// POLYGLOT GAP: .WithExternalHttpEndpoints() — for the health checks UI resource.
// POLYGLOT GAP: .WithHostPort(7230) — conditional host port in run mode for health checks UI.

await builder.build().run();
