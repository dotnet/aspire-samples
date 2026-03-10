import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const cache = builder.addRedis("cache");

const app = builder.addUvicornApp("app", "../app", "main:app")
    .withUv()
    .withExternalHttpEndpoints()
    .withReference(cache)
    .waitFor(cache)
    .withHttpHealthCheck("/health");

const frontend = builder.addViteApp("frontend", "../frontend")
    .withReference(app)
    .waitFor(app);

app.publishWithContainerFiles(frontend, "./static");

await builder.build().run();
