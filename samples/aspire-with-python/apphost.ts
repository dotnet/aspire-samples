import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

const cache = builder.addRedis("cache");

// POLYGLOT GAP: AddUvicornApp("app", "./app", "main:app") is not available in the TypeScript polyglot SDK.
// POLYGLOT GAP: .WithUv() — UV package manager configuration is not available.
// POLYGLOT GAP: .WithExternalHttpEndpoints().WithReference(cache).WaitFor(cache).WithHttpHealthCheck("/health")
// The Python/Uvicorn app cannot be added directly.

// POLYGLOT GAP: AddViteApp("frontend", "./frontend") is not available in the TypeScript polyglot SDK.
// POLYGLOT GAP: app.PublishWithContainerFiles(frontend, "./static") — PublishWithContainerFiles is not available.

await builder.build().run();
