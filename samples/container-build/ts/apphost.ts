// Setup: No additional packages required (uses core container and Dockerfile APIs).

import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

const goVersion = builder.addParameter("goversion", { default: "1.25.4" });

const execCtx = await builder.executionContext.get();
const isRunMode = await execCtx.isRunMode.get();
const isPublishMode = !isRunMode;

let ginapp;
if (isPublishMode) {
    ginapp = builder.addDockerfile("ginapp", "../ginapp")
        .withBuildArg("GO_VERSION", goVersion);
} else {
    ginapp = builder.addDockerfile("ginapp", "../ginapp", "Dockerfile.dev")
        .withBuildArg("GO_VERSION", goVersion)
        .withBindMount("../ginapp", "/app");
}

ginapp
    .withHttpEndpoint({ targetPort: 5555, env: "PORT" })
    .withHttpHealthCheck("/")
    .withExternalHttpEndpoints()
    .withOtlpExporter();
// POLYGLOT GAP: .withDeveloperCertificateTrust(true) — developer certificate trust may not be available.

if (isPublishMode) {
    ginapp
        .withEnvironment("GIN_MODE", "release")
        .withEnvironment("TRUSTED_PROXIES", "all");
}

await builder.build().run();
