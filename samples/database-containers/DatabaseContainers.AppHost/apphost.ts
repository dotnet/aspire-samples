// Setup: Run the following commands to add required integrations:
//   aspire add postgres
//   aspire add mysql
//   aspire add sqlserver

import { createBuilder, ContainerLifetime } from "./.modules/aspire.js";
import { readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const builder = await createBuilder();

const todosDbName = "Todos";

const postgres = await builder.addPostgres("postgres")
    .withEnvironment("POSTGRES_DB", todosDbName)
    .withBindMount("../DatabaseContainers.ApiService/data/postgres", "/docker-entrypoint-initdb.d")
    .withDataVolume()
    .withPgWeb()
    .withLifetime(ContainerLifetime.Persistent);

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

// Read the SQL creation script and apply it to the database
const __dirname = dirname(fileURLToPath(import.meta.url));
const initSql = readFileSync(join(__dirname, "init.sql"), "utf-8");
const addressBookDb = sqlserver.addDatabase("AddressBook")
    .withCreationScript(initSql);

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
