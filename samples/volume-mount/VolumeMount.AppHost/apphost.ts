import { createBuilder, ContainerLifetime } from "./.modules/aspire.js";

const builder = await createBuilder();

const sqlserver = await builder.addSqlServer("sqlserver")
    .withDataVolume()
    .withLifetime(ContainerLifetime.Persistent);

const sqlDatabase = sqlserver.addDatabase("sqldb");

// POLYGLOT GAP: AddAzureStorage("Storage").RunAsEmulator(emulator => emulator.WithDataVolume()) — Azure Storage emulator
// with callback configuration is not available in the TypeScript polyglot SDK.
// POLYGLOT GAP: .AddBlobs("BlobConnection") — Azure Blob storage integration is not available.
// const blobs = builder.addAzureStorage("Storage").runAsEmulator(...).addBlobs("BlobConnection");

// POLYGLOT GAP: AddProject<Projects.VolumeMount_BlazorWeb>("blazorweb") — generic type parameter for project reference is not available.
const blazorweb = builder.addProject("blazorweb")
    .withReference(sqlDatabase)
    .waitFor(sqlDatabase);
// POLYGLOT GAP: .withReference(blobs).waitFor(blobs) — Azure Blob resource reference cannot be added (see Azure Storage gap above).

await builder.build().run();
