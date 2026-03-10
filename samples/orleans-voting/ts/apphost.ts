import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const redis = await builder.addRedis("voting-redis");

const orleans = await builder.addOrleans("voting-cluster")
    .withClustering(redis)
    .withGrainStorage("votes", redis);

const votingFe = await builder.addCSharpAppWithOptions("voting-fe", "../OrleansVoting.Service/OrleansVoting.Service.csproj", async (opts) => {})
    .withReplicas(3)
    .withOrleansReference(orleans)
    .waitFor(redis)
    .withExternalHttpEndpoints();

await builder.build().run();
