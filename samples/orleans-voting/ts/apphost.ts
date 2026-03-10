// Setup: Run the following commands to add required integrations:
//   aspire add redis
//   aspire add orleans

import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

const redis = builder.addRedis("voting-redis");

const orleans = builder.addOrleans("voting-cluster")
    .withClustering(redis)
    .withGrainStorage("votes", redis);

const votingFe = builder.addProject("voting-fe")
    .withReference(orleans)
    .waitFor(redis)
    .withReplicas(3)
    .withExternalHttpEndpoints();
// POLYGLOT GAP: .WithUrlForEndpoint callbacks for display text/location are not available.

await builder.build().run();
