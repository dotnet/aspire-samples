import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

const cache = await builder.addRedis("cache")
    .withRedisInsight();

// POLYGLOT GAP: AddProject<Projects.AspireWithNode_AspNetCoreApi>("weatherapi") — generic type parameter for project reference is not available.
const weatherapi = builder.addProject("weatherapi")
    .withHttpHealthCheck("/health");

// POLYGLOT GAP: AddNodeApp("frontend", "../NodeFrontend", "./app.js") is not available in the TypeScript polyglot SDK.
// POLYGLOT GAP: .WithNpm() — npm configuration is not available.
// POLYGLOT GAP: .WithRunScript("dev") — run script configuration is not available.
// POLYGLOT GAP: .WithHttpEndpoint(port: 5223, env: "PORT") — WithHttpEndpoint with env parameter is not available.
// The following Node.js app cannot be added directly:
// builder.AddNodeApp("frontend", "../NodeFrontend", "./app.js").WithNpm().WithRunScript("dev")
//   .WithHttpEndpoint(port: 5223, env: "PORT").WithExternalHttpEndpoints().WithHttpHealthCheck("/health")
//   .WithReference(weatherapi).WaitFor(weatherapi).WithReference(cache).WaitFor(cache)

await builder.build().run();
