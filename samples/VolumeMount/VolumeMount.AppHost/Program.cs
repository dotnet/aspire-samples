var builder = DistributedApplication.CreateBuilder(args);

var sqlDatabase = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .AddDatabase("sqldb");

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
