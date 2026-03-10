import { ContainerLifetime, createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const sqlserver = await builder.addSqlServer("sqlserver")
    .withDataVolume()
    .withLifetime(ContainerLifetime.Persistent);

const db1 = await sqlserver.addDatabase("db1");

const migrationService = await builder.addProject("migration", "../DatabaseMigrations.MigrationService/DatabaseMigrations.MigrationService.csproj", "https")
    .withReference(db1)
    .waitFor(db1);

await builder.addProject("api", "../DatabaseMigrations.ApiService/DatabaseMigrations.ApiService.csproj", "https")
    .withReference(db1)
    .waitForCompletion(migrationService);

await builder.build().run();
