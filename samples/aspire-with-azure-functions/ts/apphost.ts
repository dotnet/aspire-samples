import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

await builder.addAzureContainerAppEnvironment("env");

const storage = await builder.addAzureStorage("storage")
    .runAsEmulator();

const blobs = await storage.addBlobs("blobs");
const queues = await storage.addQueues("queues");

const functions = await builder.addAzureFunctionsProject("functions", "../ImageGallery.Functions/ImageGallery.Functions.csproj")
    .withReference(queues)
    .withReference(blobs)
    .waitFor(storage)
    .withHostStorage(storage);

await builder.addProject("frontend", "../ImageGallery.FrontEnd/ImageGallery.FrontEnd.csproj", "https")
    .withReference(queues)
    .withReference(blobs)
    .waitFor(functions)
    .withExternalHttpEndpoints();

await builder.build().run();
