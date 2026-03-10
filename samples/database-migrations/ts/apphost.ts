// Setup: Run the following commands to add required integrations:
//   aspire add sqlserver

import { createBuilder, ContainerLifetime } from "./.modules/aspire.js";

const builder = await createBuilder();

const sqlserver = await builder.addSqlServer("sqlserver")
    .withDataVolume()
    .withLifetime(ContainerLifetime.Persistent);

const db1 = sqlserver.addDatabase("db1");

const migrationService = builder.addProject("migration")
    .withReference(db1)
    .waitFor(db1);

const api = builder.addProject("api")
    .withReference(db1)
    .waitForCompletion(migrationService);

await builder.build().run();
