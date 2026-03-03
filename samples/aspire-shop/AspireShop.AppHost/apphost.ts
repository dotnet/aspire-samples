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
// POLYGLOT GAP: AddProject<Projects.AspireShop_CatalogDbManager>("catalogdbmanager") — generic type parameter for project reference is not available; use addProject("name") instead.
// POLYGLOT GAP: .WithHttpCommand("/reset-db", "Reset Database", commandOptions: new() { IconName = "DatabaseLightning" }) — custom HTTP commands are not available in the TypeScript SDK.

const catalogService = builder.addProject("catalogservice")
    .withReference(catalogDb)
    .waitFor(catalogDbManager)
    .withHttpHealthCheck("/health");
// POLYGLOT GAP: AddProject<Projects.AspireShop_CatalogService>("catalogservice") — generic type parameter for project reference is not available.

const basketService = builder.addProject("basketservice")
    .withReference(basketCache)
    .waitFor(basketCache);
// POLYGLOT GAP: AddProject<Projects.AspireShop_BasketService>("basketservice") — generic type parameter for project reference is not available.

const frontend = builder.addProject("frontend")
    .withExternalHttpEndpoints()
    .withHttpHealthCheck("/health")
    .withReference(basketService)
    .withReference(catalogService)
    .waitFor(catalogService);
// POLYGLOT GAP: AddProject<Projects.AspireShop_Frontend>("frontend") — generic type parameter for project reference is not available.
// POLYGLOT GAP: .WithUrlForEndpoint("https", url => url.DisplayText = "Online Store (HTTPS)") — lambda URL customization is not available.
// POLYGLOT GAP: .WithUrlForEndpoint("http", url => url.DisplayText = "Online Store (HTTP)") — lambda URL customization is not available.

await builder.build().run();
