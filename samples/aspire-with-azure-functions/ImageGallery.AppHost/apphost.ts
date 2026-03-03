// Setup: Run the following commands to add required integrations:
//   aspire add azure-appcontainers
//   aspire add azure-storage
//   aspire add azure-functions

import { createBuilder, StorageBuiltInRole } from "./.modules/aspire.js";

const builder = await createBuilder();

builder.addAzureContainerAppEnvironment("env");

const storage = builder.addAzureStorage("storage")
    .runAsEmulator();
// POLYGLOT GAP: .ConfigureInfrastructure(infra => { ... }) — Bicep infrastructure configuration
// with Azure.Provisioning.Storage.StorageAccount is a C# lambda and not directly available.
// The storage account will use default settings.

const blobs = storage.addBlobs("blobs");
const queues = storage.addQueues("queues");

const functions = builder.addAzureFunctionsProject("functions")
    .withReference(queues)
    .withReference(blobs)
    .waitFor(storage)
    .withRoleAssignments(storage,
        StorageBuiltInRole.StorageAccountContributor,
        StorageBuiltInRole.StorageBlobDataOwner,
        StorageBuiltInRole.StorageQueueDataContributor)
    .withHostStorage(storage);
// POLYGLOT GAP: .WithUrlForEndpoint("http", u => u.DisplayText = "Functions App") — lambda URL customization is not available.

const frontend = builder.addProject("frontend")
    .withReference(queues)
    .withReference(blobs)
    .waitFor(functions)
    .withExternalHttpEndpoints();
// POLYGLOT GAP: .WithUrlForEndpoint callbacks for display text are not available.

await builder.build().run();
