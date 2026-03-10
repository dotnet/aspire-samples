import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const postgres = builder.addPostgres("postgres")
    .withPgAdmin()
    .withLifetime("persistent");

if (builder.executionContext.isRunMode) {
    await postgres.withDataVolume();
}

const catalogDb = postgres.addDatabase("catalogdb");

const basketCache = builder.addRedis("basketcache")
    .withDataVolume()
    .withRedisCommander();

const catalogDbManager = builder.addProject("catalogdbmanager", "../AspireShop.CatalogDbManager/AspireShop.CatalogDbManager.csproj", "https")
    .withReference(catalogDb)
    .waitFor(catalogDb)
    .withHttpHealthCheck("/health");

const catalogService = builder.addProject("catalogservice", "../AspireShop.CatalogService/AspireShop.CatalogService.csproj", "https")
    .withReference(catalogDb)
    .waitFor(catalogDbManager)
    .withHttpHealthCheck("/health");

const basketService = builder.addProject("basketservice", "../AspireShop.BasketService/AspireShop.BasketService.csproj", "https")
    .withReference(basketCache)
    .waitFor(basketCache);

builder.addProject("frontend", "../AspireShop.Frontend/AspireShop.Frontend.csproj", "https")
    .withExternalHttpEndpoints()
    .withHttpHealthCheck("/health")
    .withReference(basketService)
    .withReference(catalogService)
    .waitFor(catalogService);

await builder.build().run();
