var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL container is configured to use trust authentication by default so no password is required
// and supports setting the default database name via an environment variable & running *.sql/*.sh scripts in a bind mount.
var todosDbName = "Todos";
var todosDb = builder.AddPostgresContainer("postgres")
    // Set the name of the default database to auto-create on container startup.
    .WithEnvironment("POSTGRES_DB", todosDbName)
    // Mount the SQL scripts directory into the container so that the init scripts run.
    .WithVolumeMount("../DatabaseContainers.ApiService/data/postgres", "/docker-entrypoint-initdb.d", VolumeMountType.Bind)
    // Add the default database to the application model so that it can be referenced by other resources.
    .AddDatabase(todosDbName);

// MySql container is configured with an auto-generated password by default
// and supports setting the default database name via an environment variable & running *.sql/*.sh scripts in a bind mount.
var catalogDbName = "catalog"; // MySql database & table names are case-sensitive on non-Windows.
var catalogDb = builder.AddMySqlContainer("mysql")
    // Set the name of the database to auto-create on container startup.
    .WithEnvironment("MYSQL_DATABASE", catalogDbName)
    // Mount the SQL scripts directory into the container so that the init scripts run.
    .WithVolumeMount("../DatabaseContainers.ApiService/data/mysql", "/docker-entrypoint-initdb.d", VolumeMountType.Bind)
    // Add the database to the application model so that it can be referenced by other resources.
    .AddDatabase(catalogDbName);

// SQL Server container is configured with an auto-generated password by default
// but doesn't support any auto-creation of databases or running scripts on startup so we have to do it manually.
var addressBookDb = builder.AddSqlServerContainer("sqlserver")
    // Mount the init scripts directory into the container.
    .WithVolumeMount("./sqlserverconfig", "/usr/config", VolumeMountType.Bind)
    // Mount the SQL scripts directory into the container so that the init scripts run.
    .WithVolumeMount("../DatabaseContainers.ApiService/data/sqlserver", "/docker-entrypoint-initdb.d", VolumeMountType.Bind)
    // Run the custom entrypoint script on startup.
    .WithArgs("/usr/config/entrypoint.sh")
    // Add the database to the application model so that it can be referenced by other resources.
    .AddDatabase("AddressBook");

builder.AddProject<Projects.DatabaseContainers_ApiService>("apiservice")
    .WithReference(todosDb)
    .WithReference(catalogDb)
    .WithReference(addressBookDb);

builder.Build().Run();
