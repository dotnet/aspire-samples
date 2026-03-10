import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const goVersion = await builder.addParameter("goversion", { default: "1.25.4" });

const context = await builder.executionContext.get();
const isPublish = await context.isPublishMode.get();

let ginapp;

if (isPublish) {
    ginapp = await builder.addDockerfile("ginapp", "../ginapp")
        .withBuildArg("GO_VERSION", goVersion);
} else {
    ginapp = await builder.addDockerfile("ginapp", "../ginapp", { dockerfilePath: "Dockerfile.dev" })
        .withBuildArg("GO_VERSION", goVersion)
        .withBindMount("../ginapp", "/app");
}

await ginapp
    .withHttpEndpoint({ targetPort: 5555, env: "PORT" })
    .withHttpHealthCheck({
        path: "/"
    })
    .withExternalHttpEndpoints()
    .withOtlpExporter()
    .withDeveloperCertificateTrust(true);

if (isPublish) {
    await ginapp
        .withEnvironment("GIN_MODE", "release")
        .withEnvironment("TRUSTED_PROXIES", "all");
}

await builder.build().run();
