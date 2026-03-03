import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

// POLYGLOT GAP: builder.AddParameter("goversion", "1.25.4", publishValueAsDefault: true) — AddParameter with publishValueAsDefault is not available in the TypeScript SDK.
// Using a plain string as a workaround for the parameter value.
const goVersion = "1.25.4";

const execCtx = await builder.executionContext.get();
const isRunMode = await execCtx.isRunMode.get();
const isPublishMode = !isRunMode;

// POLYGLOT GAP: AddDockerfile("ginapp", "./ginapp") and AddDockerfile("ginapp", "./ginapp", "Dockerfile.dev") — AddDockerfile is not available in the TypeScript polyglot SDK.
// POLYGLOT GAP: .WithBuildArg("GO_VERSION", goVersion) — WithBuildArg is not available.
// POLYGLOT GAP: .WithOtlpExporter() — OTLP exporter configuration is not available.
// POLYGLOT GAP: .WithDeveloperCertificateTrust(trust: true) — developer certificate trust is not available.
//
// The following Dockerfile-based container cannot be added directly:
// let ginapp;
// if (isPublishMode) {
//   ginapp = builder.addDockerfile("ginapp", "./ginapp").withBuildArg("GO_VERSION", goVersion);
// } else {
//   ginapp = builder.addDockerfile("ginapp", "./ginapp", "Dockerfile.dev")
//     .withBuildArg("GO_VERSION", goVersion).withBindMount("./ginapp", "/app");
// }
// ginapp.withHttpEndpoint({ targetPort: 5555, env: "PORT" })
//   .withHttpHealthCheck("/").withExternalHttpEndpoints()
//   .withOtlpExporter().withDeveloperCertificateTrust(true);
// if (isPublishMode) {
//   ginapp.withEnvironment("GIN_MODE", "release").withEnvironment("TRUSTED_PROXIES", "all");
// }

await builder.build().run();
