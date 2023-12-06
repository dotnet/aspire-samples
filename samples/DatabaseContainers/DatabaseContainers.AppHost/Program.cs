using System.Net.Sockets;
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// A password is required for the SQL Server container and in preview.1 there's a bug with the default generated password failing
// the complexity requirements so we create a random password here.
var sqlserverDb = builder.AddSqlServerContainer("sqlserver", Guid.NewGuid().ToString("N"))
    // Mount the SQL scripts directory into the container so that the init scripts run.
    .WithVolumeMount("../DatabaseContainers.ApiService/data/sqlserver", "/docker-entrypoint-initdb.d", VolumeMountType.Bind)
    // Add the database to the application model so that it can be referenced by other resources.
    .AddDatabase("sqlserverdb");

// Add an executable resource that will run sqlcmd inside the SQL Server container to initialize the database.
// Ideally we could simply override the entrypoint of the SQL Server container to run our own init shell script but that's not supported.
// TODO: This needs to be resilient to the target container not being up yet, e.g. move to a shell script with a retry loop.
builder.AddExecutable("sqlserverdbinit", "docker", workingDirectory: "", args: ["exec", "-it", "<container_id>", "sqlcmd", "-S", "$SQL_ADDRESS", "-U", "sa", "-P", sqlserverDb.Resource.Parent.Password, "-i", "/docker-entrypoint-initdb.d/init.sql"])
    .WithEnvironment("SQL_ADDRESS", () =>
    {
        if (!sqlserverDb.Resource.Parent.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint for SQL Server.
        return allocatedEndpoint.Address;
    })
    .WithEnvironment("SA_PASSWORD", sqlserverDb.Resource.Parent.Password);

// PostgreSQL container is configured to use trust authentication by default so no password is required.
var postgresDbName = "postgresdb";
var postgresDb = builder.AddPostgresContainer("postgres")
    // Set the name of the default database to auto-create on container startup.
    .WithEnvironment("POSTGRES_DB", postgresDbName)
    // Mount the SQL scripts directory into the container so that the init scripts run.
    .WithVolumeMount("../DatabaseContainers.ApiService/data/postgres", "/docker-entrypoint-initdb.d", VolumeMountType.Bind)
    // Add the default database to the application model so that it can be referenced by other resources.
    .AddDatabase(postgresDbName);

// MySql is a custom container resource defined in this project showing how to create a custom database container resource.
var mysqlDbName = "mysqldb";
var mysqlDb = builder.AddMySqlContainer("mysql")
    // Set the name of the database to auto-create on container startup.
    .WithEnvironment("MYSQL_DATABASE", mysqlDbName)
    // Mount the SQL scripts directory into the container so that the init scripts run.
    .WithVolumeMount("../DatabaseContainers.ApiService/data/mysql", "/docker-entrypoint-initdb.d", VolumeMountType.Bind)
    // Add the database to the application model so that it can be referenced by other resources.
    .AddDatabase(mysqlDbName);

var apiservice = builder.AddProject<Projects.DatabaseContainers_ApiService>("apiservice")
    .WithReference(sqlserverDb)
    .WithReference(postgresDb)
    .WithReference(mysqlDb);

builder.AddProject<Projects.DatabaseContainers_Web>("webfrontend")
    .WithReference(apiservice);

builder.Build().Run();

class MySqlContainerResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    public string? GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint for MySql.

        var connectionString = $"server={allocatedEndpoint.Address};Port={allocatedEndpoint.Port};uid=mysql;pwd=";
        return connectionString;
    }
}

class MySqlDatabaseResource(string name, MySqlContainerResource parent) : IResourceWithConnectionString
{
    public MySqlContainerResource Parent { get; } = parent;
    public string Name { get; } = name;
    public ResourceMetadataCollection Annotations { get; } = [];

    public string? GetConnectionString()
    {
        var connectionString = $"{Parent.GetConnectionString()};Database={Name}";
        return connectionString;
    }
}

static class MySqlContainerExtensions
{
    public static IResourceBuilder<MySqlContainerResource> AddMySqlContainer(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        return builder.AddResource(new MySqlContainerResource(name))
            .WithAnnotation(new ContainerImageAnnotation { Image = "mysql", Tag = "latest" })
            .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 3306))
            .WithEnvironment("MYSQL_ALLOW_EMPTY_PASSWORD", "true")
            .WithEnvironment("MYSQL_USER", "mysql")
            .WithEnvironment("MYSQL_PASSWORD", "");
    }

    public static IResourceBuilder<MySqlDatabaseResource> AddDatabase(this IResourceBuilder<MySqlContainerResource> builder, string name)
    {
        return builder.ApplicationBuilder.AddResource(new MySqlDatabaseResource(name, builder.Resource));
    }
}
