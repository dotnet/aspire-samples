// Setup: Run the following commands to add required integrations:
//   aspire add postgres
//   aspire add redis

import { createBuilder, ContainerLifetime } from "./.modules/aspire.js";

const builder = await createBuilder();

const postgres = await builder.addPostgres("postgres")
    .withPgAdmin()
    .withLifetime(ContainerLifetime.Persistent);

const execCtx = await builder.executionContext.get();
const isRunMode = await execCtx.isRunMode.get();
if (isRunMode) {
    await postgres.withDataVolume();
}

const catalogDb = postgres.addDatabase("catalogdb");

const basketCache = await builder.addRedis("basketcache")
    .withDataVolume()
    .withRedisCommander();

const catalogDbManager = builder.addProject("catalogdbmanager")
    .withReference(catalogDb)
    .waitFor(catalogDb)
    .withHttpHealthCheck("/health");
// POLYGLOT GAP: .WithHttpCommand("/reset-db", "Reset Database", ...) — custom HTTP commands are not available.

const catalogService = builder.addProject("catalogservice")
    .withReference(catalogDb)
    .waitFor(catalogDbManager)
    .withHttpHealthCheck("/health");

const basketService = builder.addProject("basketservice")
    .withReference(basketCache)
    .waitFor(basketCache);

const frontend = builder.addProject("frontend")
    .withExternalHttpEndpoints()
    .withHttpHealthCheck("/health")
    .withReference(basketService)
    .withReference(catalogService)
    .waitFor(catalogService);
// POLYGLOT GAP: .WithUrlForEndpoint callbacks for display text are not available.

await builder.build().run();
