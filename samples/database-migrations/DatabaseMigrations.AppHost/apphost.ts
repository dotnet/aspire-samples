import { createBuilder, ContainerLifetime } from "./.modules/aspire.js";

const builder = await createBuilder();

const sqlserver = await builder.addSqlServer("sqlserver")
    .withDataVolume()
    .withLifetime(ContainerLifetime.Persistent);

const db1 = sqlserver.addDatabase("db1");

// POLYGLOT GAP: AddProject<Projects.DatabaseMigrations_MigrationService>("migration") — generic type parameter for project reference is not available.
const migrationService = builder.addProject("migration")
    .withReference(db1)
    .waitFor(db1);

// POLYGLOT GAP: AddProject<Projects.DatabaseMigrations_ApiService>("api") — generic type parameter for project reference is not available.
// POLYGLOT GAP: .WaitForCompletion(migrationService) — WaitForCompletion is not available in the TypeScript polyglot SDK; only WaitFor is available.
const api = builder.addProject("api")
    .withReference(db1)
    .waitFor(migrationService);

await builder.build().run();
