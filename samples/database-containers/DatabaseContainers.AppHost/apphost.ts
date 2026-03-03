import { createBuilder, ContainerLifetime } from "./.modules/aspire.js";

const builder = await createBuilder();

const todosDbName = "Todos";

const postgres = await builder.addPostgres("postgres")
    .withEnvironment("POSTGRES_DB", todosDbName)
    .withBindMount("../DatabaseContainers.ApiService/data/postgres", "/docker-entrypoint-initdb.d")
    .withDataVolume()
    .withLifetime(ContainerLifetime.Persistent);
// POLYGLOT GAP: .WithPgWeb() — PgWeb integration is not available in the TypeScript polyglot SDK.

const todosDb = postgres.addDatabase(todosDbName);

const catalogDbName = "catalog";

const mysql = await builder.addMySql("mysql")
    .withEnvironment("MYSQL_DATABASE", catalogDbName)
    .withBindMount("../DatabaseContainers.ApiService/data/mysql", "/docker-entrypoint-initdb.d")
    .withDataVolume()
    .withLifetime(ContainerLifetime.Persistent);

const catalogDb = mysql.addDatabase(catalogDbName);

const sqlserver = await builder.addSqlServer("sqlserver")
    .withDataVolume()
    .withLifetime(ContainerLifetime.Persistent);

// POLYGLOT GAP: WithCreationScript(File.ReadAllText(initScriptPath)) — reading a file and passing its content
// via WithCreationScript is not available in the TypeScript polyglot SDK.
// In C#: var initScriptPath = Path.Join(Path.GetDirectoryName(typeof(Program).Assembly.Location), "init.sql");
// var addressBookDb = sqlserver.AddDatabase("AddressBook").WithCreationScript(File.ReadAllText(initScriptPath));
const addressBookDb = sqlserver.addDatabase("AddressBook");

// POLYGLOT GAP: AddProject<Projects.DatabaseContainers_ApiService>("apiservice") — generic type parameter for project reference is not available.
const apiservice = builder.addProject("apiservice")
    .withReference(todosDb)
    .waitFor(todosDb)
    .withReference(catalogDb)
    .waitFor(catalogDb)
    .withReference(addressBookDb)
    .waitFor(addressBookDb)
    .withHttpHealthCheck("/alive")
    .withHttpHealthCheck("/health");

await builder.build().run();
