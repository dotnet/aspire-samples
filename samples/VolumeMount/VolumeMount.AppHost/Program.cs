using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Using a persistent volume mount requires a stable password for 'sa' rather than the default generated one.
var sqlpassword = builder.Configuration["sqlpassword"];

if (builder.Environment.IsDevelopment() && string.IsNullOrEmpty(sqlpassword))
{
    throw new InvalidOperationException("""
        A password for the local SQL Server container is not configured.
        Add one to the AppHost project's user secrets with the key 'sqlpassword', e.g. dotnet user-secrets set sqlpassword <password>
        """);
}

// To have a persistent volume mount across container instances, it must be named (VolumeMountType.Named).
var database = builder.AddSqlServerContainer("sqlserver", sqlpassword)
    .WithVolumeMount("VolumeMount.sqlserver.data", "/var/opt/mssql", VolumeMountType.Named)
    .AddDatabase("appdb");

builder.AddProject<Projects.VolumeMount_BlazorWeb>("blazorweb")
    .WithReference(database);

builder.Build().Run();
