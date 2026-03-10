import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const sqlserver = builder.addSqlServer("sqlserver")
    .withDataVolume()
    .withLifetime("persistent");

const db1 = sqlserver.addDatabase("db1");

const migrationService = builder.addProject("migration", "../DatabaseMigrations.MigrationService/DatabaseMigrations.MigrationService.csproj", "https")
    .withReference(db1)
    .waitFor(db1);

builder.addProject("api", "../DatabaseMigrations.ApiService/DatabaseMigrations.ApiService.csproj", "https")
    .withReference(db1)
    .waitForCompletion(migrationService);

await builder.build().run();
