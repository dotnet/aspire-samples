import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

// POLYGLOT GAP: AddAzureContainerAppEnvironment("env") — Azure Container App environment is not available in the TypeScript polyglot SDK.
// builder.addAzureContainerAppEnvironment("env");

// POLYGLOT GAP: AddAzureStorage("storage").RunAsEmulator() — Azure Storage emulator integration is not available.
// POLYGLOT GAP: .ConfigureInfrastructure(infra => { ... }) — Bicep infrastructure configuration with
//   Azure.Provisioning.Storage.StorageAccount and BlobService properties is not available.
// const storage = builder.addAzureStorage("storage").runAsEmulator();

// POLYGLOT GAP: storage.AddBlobs("blobs") — Azure Blob storage integration is not available.
// POLYGLOT GAP: storage.AddQueues("queues") — Azure Queue storage integration is not available.
// const blobs = storage.addBlobs("blobs");
// const queues = storage.addQueues("queues");

// POLYGLOT GAP: AddAzureFunctionsProject<Projects.ImageGallery_Functions>("functions") — Azure Functions project integration is not available.
// POLYGLOT GAP: .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataContributor, ...) — Azure role assignments are not available.
// POLYGLOT GAP: .WithHostStorage(storage) — host storage configuration is not available.
// const functions = builder.addAzureFunctionsProject("functions")
//   .withReference(queues).withReference(blobs).waitFor(storage)
//   .withRoleAssignments(storage, ...).withHostStorage(storage);

// POLYGLOT GAP: AddProject<Projects.ImageGallery_FrontEnd>("frontend") — generic type parameter for project reference is not available.
// POLYGLOT GAP: Full functionality requires Azure Storage, Queues, Blobs, and Functions project references above.
const frontend = builder.addProject("frontend")
    .withExternalHttpEndpoints();
// POLYGLOT GAP: .withReference(queues).withReference(blobs).waitFor(functions) — cannot reference Azure resources (see gaps above).

await builder.build().run();
