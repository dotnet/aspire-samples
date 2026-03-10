import { createBuilder } from './.modules/aspire.js';
import { readFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const builder = await createBuilder();

// PostgreSQL
const todosDbName = "Todos";

const postgres = builder.addPostgres("postgres")
    .withEnvironment("POSTGRES_DB", todosDbName)
    .withBindMount("../DatabaseContainers.ApiService/data/postgres", "/docker-entrypoint-initdb.d")
    .withDataVolume()
    .withPgWeb()
    .withLifetime("persistent");

const todosDb = postgres.addDatabase(todosDbName);

// MySQL
const catalogDbName = "catalog";

const mysql = builder.addMySql("mysql")
    .withEnvironment("MYSQL_DATABASE", catalogDbName)
    .withBindMount("../DatabaseContainers.ApiService/data/mysql", "/docker-entrypoint-initdb.d")
    .withDataVolume()
    .withLifetime("persistent");

const catalogDb = mysql.addDatabase(catalogDbName);

// SQL Server
const sqlserver = builder.addSqlServer("sqlserver")
    .withDataVolume()
    .withLifetime("persistent");

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const initScriptPath = join(__dirname, "../DatabaseContainers.ApiService/data/sqlserver/init.sql");
const addressBookDb = sqlserver.addDatabase("AddressBook")
    .withCreationScript(readFileSync(initScriptPath, "utf-8"));

builder.addProject("apiservice", "../DatabaseContainers.ApiService/DatabaseContainers.ApiService.csproj", "https")
    .withReference(todosDb)
    .waitFor(todosDb)
    .withReference(catalogDb)
    .waitFor(catalogDb)
    .withReference(addressBookDb)
    .waitFor(addressBookDb)
    .withHttpHealthCheck("/alive");

await builder.build().run();
