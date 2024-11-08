var builder = DistributedApplication.CreateBuilder(args);

var sqlserver = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var sqlDatabase = sqlserver.AddDatabase("sqldb");

var postgresserver = builder.AddPostgres("postgresserver")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var postgresDatabase = postgresserver.AddDatabase("postgres");

var blobs = builder.AddAzureStorage("Storage")
    // Use the Azurite storage emulator for local development
    .RunAsEmulator(emulator => emulator.WithDataVolume())
    .AddBlobs("BlobConnection");

builder.AddProject<Projects.VolumeMount_BlazorWeb>("blazorweb")
    .WithReference(sqlDatabase)
    .WaitFor(sqlDatabase)
    .WithReference(postgresDatabase)
    .WaitFor(postgresDatabase)
    .WithReference(blobs)
    .WaitFor(blobs);

builder.Build().Run();
