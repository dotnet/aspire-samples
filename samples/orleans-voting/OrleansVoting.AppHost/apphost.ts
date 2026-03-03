import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

const redis = builder.addRedis("voting-redis");

// POLYGLOT GAP: AddOrleans("voting-cluster") — Orleans integration is not available in the TypeScript polyglot SDK.
// POLYGLOT GAP: .WithClustering(redis) — Orleans clustering configuration is not available.
// POLYGLOT GAP: .WithGrainStorage("votes", redis) — Orleans grain storage configuration is not available.
// const orleans = builder.addOrleans("voting-cluster").withClustering(redis).withGrainStorage("votes", redis);

// POLYGLOT GAP: AddProject<Projects.OrleansVoting_Service>("voting-fe") — generic type parameter for project reference is not available.
// POLYGLOT GAP: .WithReference(orleans) — Orleans resource reference is not available.
// POLYGLOT GAP: .WithUrlForEndpoint("https", u => u.DisplayText = "Voting App") — lambda URL customization is not available.
// POLYGLOT GAP: .WithUrlForEndpoint("http", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly) — lambda URL customization is not available.
// POLYGLOT GAP: .WithUrlForEndpoint("orleans-gateway", ...) — lambda URL customization is not available.
// POLYGLOT GAP: .WithUrlForEndpoint("orleans-silo", ...) — lambda URL customization is not available.
const votingFe = builder.addProject("voting-fe")
    .waitFor(redis)
    .withReplicas(3)
    .withExternalHttpEndpoints();

await builder.build().run();
