var builder = DistributedApplication.CreateBuilder(args);

// Using a persistent volume mount requires a stable password rather than the default generated one.
var sqlpw = builder.Configuration["sqlpassword"];
var postgrespw = builder.Configuration["postgrespassword"];

if (builder.ExecutionContext.IsRunMode && string.IsNullOrEmpty(sqlpw))
{
    throw new InvalidOperationException("""
        A password for the local SQL Server container is not configured.
        Add one to the AppHost project's user secrets with the key 'sqlpassword', e.g. dotnet user-secrets set sqlpassword <password>
        """);
}

if (builder.ExecutionContext.IsRunMode && string.IsNullOrEmpty(postgrespw))
{
    throw new InvalidOperationException("""
        A password for the local PostgreSQL container is not configured.
        Add one to the AppHost project's user secrets with the key 'postgrespassword', e.g. dotnet user-secrets set postgrespassword <password>
        """);
}

// To have a persistent volume across container instances, it must be named.
var sqlDatabase = builder.AddSqlServer("sqlserver", password: sqlpw)
    .WithVolumeMount("VolumeMount.sqlserver.data", "/var/opt/mssql")
    .AddDatabase("sqldb");

// Postgres must also have a stable password and a named volume
var postgresDatabase = builder.AddPostgres("postgres", password: postgrespw)
    .WithVolumeMount("VolumeMount.postgres.data", "/var/lib/postgresql/data")
    .AddDatabase("postgresdb");

var blobs = builder.AddAzureStorage("Storage")
    // Use the Azurite storage emulator for local development
    .RunAsEmulator(emulator => emulator.WithVolumeMount("VolumeMount.azurite.data", "/data"))
    .AddBlobs("BlobConnection");

builder.AddProject<Projects.VolumeMount_BlazorWeb>("blazorweb")
    .WithReference(sqlDatabase)
    .WithReference(postgresDatabase)
    .WithReference(blobs);

builder.Build().Run();
