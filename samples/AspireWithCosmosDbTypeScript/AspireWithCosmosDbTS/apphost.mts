import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

// Add Azure Cosmos DB and run as the local emulator during development.
// Configure the emulator to use a fixed gateway port and report 127.0.0.1
// so the Python SDK can connect without being redirected to the container's
// internal IP address.
const cosmos = builder.addAzureCosmosDB("cosmos").runAsEmulator({
    configureContainer: async (emulator) => {
        await emulator.withGatewayPort({ port: 8081 });
        await emulator.withEnvironment("AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE", "127.0.0.1");
    }
});

// Add a database named "tododb" to the Cosmos DB account.
const tododb = cosmos.addCosmosDatabase("tododb");

// Run the Python FastAPI app and expose its HTTP endpoint externally.
// Pass the Cosmos DB connection to the API so it can store TODO items.
const app = await builder
    .addUvicornApp("app", "./app", "main:app")
    .withUv()
    .withExternalHttpEndpoints()
    .withHttpHealthCheck({ path: "/health" })
    .withReference(cosmos)
    .waitFor(tododb);

// Run the Vite frontend after the API and inject the API URL for local proxying.
const frontend = await builder
    .addViteApp("frontend", "./frontend")
    .withReference(app)
    .waitFor(app);

// Bundle the frontend build output into the API container for publish/deploy.
await app.publishWithContainerFiles(frontend, "./static");

await builder.build().run();
