import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

// No persistent volume: use disposable data. The authenticated Redis REPL is for trusted dashboard users only.
const redis = await builder.addRedis('redis').withRepl();

// Local demo: anyone who can reach this unauthenticated HTTP API can read, replace, or delete demo notes.
const api = await builder.addNodeApp('api', './api', 'src/index.ts')
    .withEnvironment('NODE_OPTIONS', '--import tsx --import ./src/instrumentation.ts')
    .withHttpEndpoint({ env: 'PORT' })
    .withHttpHealthCheck({ path: '/health' })
    .withReference(redis)
    .withOtlpExporter()
    .waitFor(redis);

await builder.addDockerfile('slumber', './slumber')
    .withEnvironment('BASE_URL', api.getEndpoint('http'))
    .waitFor(api)
    .withTerminal();

await builder.build().run();