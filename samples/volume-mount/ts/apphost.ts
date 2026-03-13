import { ContainerLifetime, createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const sqlserver = await builder.addSqlServer("sqlserver")
    .withDataVolume()
    .withLifetime(ContainerLifetime.Persistent);

const sqlDatabase = await sqlserver.addDatabase("sqldb");

const blobs = await builder.addAzureStorage("Storage")
    .runAsEmulator()
    .addBlobs("BlobConnection");

await builder.addProject("blazorweb", "../VolumeMount.BlazorWeb/VolumeMount.BlazorWeb.csproj", "https")
    .withReference(sqlDatabase)
    .waitFor(sqlDatabase)
    .withReference(blobs)
    .waitFor(blobs);

await builder.build().run();
