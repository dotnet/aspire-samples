import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

builder.addAzureContainerAppEnvironment("env");

const storage = builder.addAzureStorage("storage")
    .runAsEmulator();

const blobs = storage.addBlobs("blobs");
const queues = storage.addQueues("queues");

const functions = builder.addAzureFunctionsProject("functions", "../ImageGallery.Functions/ImageGallery.Functions.csproj")
    .withReference(queues)
    .withReference(blobs)
    .waitFor(storage)
    .withHostStorage(storage);

builder.addProject("frontend", "../ImageGallery.FrontEnd/ImageGallery.FrontEnd.csproj", "https")
    .withReference(queues)
    .withReference(blobs)
    .waitFor(functions)
    .withExternalHttpEndpoints();

await builder.build().run();
