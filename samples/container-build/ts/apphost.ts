import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const goVersion = builder.addParameter("goversion", { default: "1.25.4" });

let ginapp;

if (builder.executionContext.isPublishMode) {
    ginapp = builder.addDockerfile("ginapp", "../ginapp")
        .withBuildArg("GO_VERSION", goVersion);
} else {
    ginapp = builder.addDockerfile("ginapp", "../ginapp", { dockerfilePath: "Dockerfile.dev" })
        .withBuildArg("GO_VERSION", goVersion)
        .withBindMount("../ginapp", "/app");
}

ginapp
    .withHttpEndpoint({ targetPort: 5555, env: "PORT" })
    .withHttpHealthCheck("/")
    .withExternalHttpEndpoints()
    .withOtlpExporter()
    .withDeveloperCertificateTrust();

if (builder.executionContext.isPublishMode) {
    await ginapp
        .withEnvironment("GIN_MODE", "release")
        .withEnvironment("TRUSTED_PROXIES", "all");
}

await builder.build().run();
