// Setup: Run the following commands to add required integrations:
//   aspire add redis
//   aspire add docker
//
// Note: ProbeType, addDockerComposeEnvironment, addHealthChecksUI, and withHttpProbe
// are expected to be available after aspire add docker. If ProbeType is not exported
// in the generated SDK, the withHttpProbe calls may need to be removed.

import { createBuilder, ProbeType } from "./.modules/aspire.js";

const builder = await createBuilder();

builder.addDockerComposeEnvironment("compose");

const cache = builder.addRedis("cache");

const apiService = builder.addProject("apiservice")
    .withHttpHealthCheck("/health")
    .withHttpProbe(ProbeType.Liveness, "/alive");
// POLYGLOT GAP: .WithFriendlyUrls(displayText: "API") — WithFriendlyUrls is a custom C# extension
// method defined in the AppHost project. It needs [AspireExport] to be available here.

const webFrontend = builder.addProject("webfrontend")
    .withReference(cache)
    .waitFor(cache)
    .withReference(apiService)
    .waitFor(apiService)
    .withHttpProbe(ProbeType.Liveness, "/alive")
    .withHttpHealthCheck("/health")
    .withExternalHttpEndpoints();
// POLYGLOT GAP: .WithFriendlyUrls("Web Frontend") — custom C# extension method.

const healthChecksUI = builder.addHealthChecksUI("healthchecksui")
    .withReference(apiService)
    .withReference(webFrontend)
    .withHttpProbe(ProbeType.Liveness, "/")
    .withExternalHttpEndpoints();
// POLYGLOT GAP: .WithFriendlyUrls("HealthChecksUI Dashboard", "http") — custom C# extension method.

const execCtx = await builder.executionContext.get();
const isRunMode = await execCtx.isRunMode.get();
if (isRunMode) {
    healthChecksUI.withHostPort(7230);
}

await builder.build().run();
