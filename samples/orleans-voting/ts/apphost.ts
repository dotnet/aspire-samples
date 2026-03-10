import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const redis = builder.addRedis("voting-redis");

const orleans = builder.addOrleans("voting-cluster")
    .withClustering(redis)
    .withGrainStorage("votes", redis);

builder.addProject("voting-fe", "../OrleansVoting.Service/OrleansVoting.Service.csproj", "https")
    .withReference(orleans)
    .waitFor(redis)
    .withReplicas(3)
    .withExternalHttpEndpoints();

await builder.build().run();
