import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const cache = await builder.addRedis("cache");

const app = await builder.addUvicornApp("app", "../app", "main:app")
    .withUv()
    .withExternalHttpEndpoints()
    .withReference(cache)
    .waitFor(cache)
    .withHttpHealthCheck({
        path: "/health"
    });

const frontend = await builder.addViteApp("frontend", "../frontend")
    .withServiceReference(app)
    .waitFor(app);

await app.publishWithContainerFiles(frontend, "./static");

await builder.build().run();
