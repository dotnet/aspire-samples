using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Using a persistent volume mount requires a stable password rather than the default generated one.
var sqlpw = builder.Configuration["sqlpassword"];
var postgrespw = builder.Configuration["postgrespassword"];

if (builder.Environment.IsDevelopment() && string.IsNullOrEmpty(sqlpw))
{
    throw new InvalidOperationException("""
        A password for the local SQL Server container is not configured.
        Add one to the AppHost project's user secrets with the key 'sqlpassword', e.g. dotnet user-secrets set sqlpassword <password>
        """);
}

// To have a persistent volume mount across container instances, it must be named (VolumeMountType.Named).
var sqlDatabase = builder.AddSqlServerContainer("sqlserver", sqlpw)
    .WithVolumeMount("VolumeMount.sqlserver.data", "/var/opt/mssql", VolumeMountType.Named)
    .AddDatabase("sqldb");

// Postgres must also have a stable password and a named volume
var postgresDatabase = builder.AddPostgresContainer("pg", password: postgrespw)
    .WithVolumeMount("VolumeMount.postgres.data", "/var/lib/postgresql/data", VolumeMountType.Named)
    .AddDatabase("postgresdb");

var storage = builder.AddAzureStorage("Storage");

if (builder.Environment.IsDevelopment())
{
    // Use the Azurite storage emulator for local development
    // Azurite doesn't have a WithVolumeMount method
    // We have to use the WithAnnotation method, which is what the WithVolumeMount method wraps when it is available
    storage.UseEmulator()
        .WithAnnotation(new VolumeMountAnnotation("VolumeMount.azurite.data", "/data", VolumeMountType.Named));
}

var blobs = storage.AddBlobs("BlobConnection");

builder.AddProject<Projects.VolumeMount_BlazorWeb>("blazorweb")
    .WithReference(sqlDatabase)
    .WithReference(postgresDatabase)
    .WithReference(blobs);

builder.Build().Run();
