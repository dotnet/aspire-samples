// Setup: Run the following commands to add required integrations:
//   aspire add javascript
//   aspire add redis

import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

const cache = await builder.addRedis("cache")
    .withRedisInsight();

const weatherapi = builder.addProject("weatherapi")
    .withHttpHealthCheck("/health");

const frontend = builder.addNodeApp("frontend", "../NodeFrontend", "./app.js")
    .withNpm()
    .withRunScript("dev")
    .withHttpEndpoint({ port: 5223, env: "PORT" })
    .withExternalHttpEndpoints()
    .withHttpHealthCheck("/health")
    .withReference(weatherapi)
    .waitFor(weatherapi)
    .withReference(cache)
    .waitFor(cache);

await builder.build().run();
