import { ContainerLifetime, createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const postgres = await builder.addPostgres("postgres")
    .withPgAdmin()
    .withLifetime(ContainerLifetime.Persistent);

const context = await builder.executionContext.get();
if (context.isRunMode && await context.isRunMode.get()) {
    await postgres.withDataVolume();
}

const catalogDb = await postgres.addDatabase("catalogdb");

const basketCache = await builder.addRedis("basketcache")
    .withDataVolume()
    .withRedisCommander();

const catalogDbManager = await builder.addProject("catalogdbmanager", "../AspireShop.CatalogDbManager/AspireShop.CatalogDbManager.csproj", "https")
    .withReference(catalogDb)
    .waitFor(catalogDb)
    .withHttpHealthCheck({
        path: "/health"
    });

const catalogService = await builder.addProject("catalogservice", "../AspireShop.CatalogService/AspireShop.CatalogService.csproj", "https")
    .withReference(catalogDb)
    .waitFor(catalogDbManager)
    .withHttpHealthCheck({
        path: "/health"
    });

const basketService = await builder.addProject("basketservice", "../AspireShop.BasketService/AspireShop.BasketService.csproj", "https")
    .withReference(basketCache)
    .waitFor(basketCache);

const frontend = await builder.addProject("frontend", "../AspireShop.Frontend/AspireShop.Frontend.csproj", "https")
    .withExternalHttpEndpoints()
    .withHttpHealthCheck({
        path: "/health"
    })
    .withServiceReference(basketService)
    .withServiceReference(catalogService)
    .waitFor(catalogService);

await builder.build().run();
