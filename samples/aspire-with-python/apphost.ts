// Setup: Run the following commands to add required integrations:
//   aspire add javascript
//   aspire add python
//   aspire add redis

import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

const cache = builder.addRedis("cache");

const app = builder.addUvicornApp("app", "./app", "main:app")
    .withUv()
    .withExternalHttpEndpoints()
    .withReference(cache)
    .waitFor(cache)
    .withHttpHealthCheck("/health");

const frontend = builder.addViteApp("frontend", "./frontend")
    .withReference(app)
    .waitFor(app);

// POLYGLOT GAP: app.publishWithContainerFiles(frontend, "./static") — bundling Vite output
// into the Python app's static directory may not be available.

await builder.build().run();
