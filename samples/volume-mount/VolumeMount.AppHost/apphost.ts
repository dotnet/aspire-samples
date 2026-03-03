// Setup: Run the following commands to add required integrations:
//   aspire add sqlserver
//   aspire add azure-storage

import { createBuilder, ContainerLifetime } from "./.modules/aspire.js";

const builder = await createBuilder();

const sqlserver = await builder.addSqlServer("sqlserver")
    .withDataVolume()
    .withLifetime(ContainerLifetime.Persistent);

const sqlDatabase = sqlserver.addDatabase("sqldb");

const blobs = builder.addAzureStorage("Storage")
    .runAsEmulator(emulator => emulator.withDataVolume())
    .addBlobs("BlobConnection");

const blazorweb = builder.addProject("blazorweb")
    .withReference(sqlDatabase)
    .waitFor(sqlDatabase)
    .withReference(blobs)
    .waitFor(blobs);

await builder.build().run();
