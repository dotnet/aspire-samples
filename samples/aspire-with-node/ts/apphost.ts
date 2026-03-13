import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const cache = await builder.addRedis("cache")
    .withRedisInsight();

const weatherapi = await builder.addProject("weatherapi", "../AspireWithNode.AspNetCoreApi/AspireWithNode.AspNetCoreApi.csproj", "https")
    .withHttpHealthCheck({
        path: "/health"
    });

await builder.addNodeApp("frontend", "../NodeFrontend", "./app.js")
    .withNpm()
    .withRunScript("dev")
    .withHttpEndpoint({ port: 5223, env: "PORT" })
    .withExternalHttpEndpoints()
    .withHttpHealthCheck({
        path: "/health"
    })
    .withServiceReference(weatherapi).waitFor(weatherapi)
    .withReference(cache).waitFor(cache);

await builder.build().run();
