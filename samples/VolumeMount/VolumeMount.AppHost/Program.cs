var builder = DistributedApplication.CreateBuilder(args);

// Using a persistent volume mount requires a stable password rather than the default generated one.

// To have a persistent volume across container instances, it must be named.
var sqlDatabase = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .AddDatabase("sqldb");

// Postgres must also have a stable password and a named volume
var postgresDatabase = builder.AddPostgres("postgresserver")
    .WithDataVolume()
    .AddDatabase("postgres");

var blobs = builder.AddAzureStorage("Storage")
    // Use the Azurite storage emulator for local development
    .RunAsEmulator(emulator => emulator.WithDataVolume())
    .AddBlobs("BlobConnection");

builder.AddProject<Projects.VolumeMount_BlazorWeb>("blazorweb")
    .WithReference(sqlDatabase)
    .WithReference(postgresDatabase)
    .WithReference(blobs);

builder.Build().Run();
