import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const sqlserver = builder.addSqlServer("sqlserver")
    .withDataVolume()
    .withLifetime("persistent");

const sqlDatabase = sqlserver.addDatabase("sqldb");

const blobs = builder.addAzureStorage("Storage")
    .runAsEmulator()
    .addBlobs("BlobConnection");

builder.addProject("blazorweb", "../VolumeMount.BlazorWeb/VolumeMount.BlazorWeb.csproj", "https")
    .withReference(sqlDatabase)
    .waitFor(sqlDatabase)
    .withReference(blobs)
    .waitFor(blobs);

await builder.build().run();
