import { ContainerLifetime, createBuilder } from './.modules/aspire.js';
import { readFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const builder = await createBuilder();

// PostgreSQL
const todosDbName = "Todos";

const postgres = await builder.addPostgres("postgres")
    .withEnvironment("POSTGRES_DB", todosDbName)
    .withBindMount("../DatabaseContainers.ApiService/data/postgres", "/docker-entrypoint-initdb.d")
    .withDataVolume()
    .withPgWeb()
    .withLifetime(ContainerLifetime.Persistent);

const todosDb = await postgres.addDatabase(todosDbName);

// MySQL
const catalogDbName = "catalog";

const mysql = await builder.addMySql("mysql")
    .withEnvironment("MYSQL_DATABASE", catalogDbName)
    .withBindMount("../DatabaseContainers.ApiService/data/mysql", "/docker-entrypoint-initdb.d")
    .withDataVolume()
    .withLifetime(ContainerLifetime.Persistent);

const catalogDb = await mysql.addDatabase(catalogDbName);

// SQL Server
const sqlserver = await builder.addSqlServer("sqlserver")
    .withDataVolume()
    .withLifetime(ContainerLifetime.Persistent);

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const initScriptPath = join(__dirname, "../DatabaseContainers.ApiService/data/sqlserver/init.sql");
const addressBookDb = await sqlserver.addDatabase("AddressBook")
    .withCreationScript(readFileSync(initScriptPath, "utf-8"));

await builder.addProject("apiservice", "../DatabaseContainers.ApiService/DatabaseContainers.ApiService.csproj", "https")
    .withReference(todosDb)
    .waitFor(todosDb)
    .withReference(catalogDb)
    .waitFor(catalogDb)
    .withReference(addressBookDb)
    .waitFor(addressBookDb)
    .withHttpHealthCheck({
        path: "/alive"
    });

await builder.build().run();
