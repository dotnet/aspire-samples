// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var sqlserver = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent);

var db1 = sqlserver.AddDatabase("db1");

var migrationService = builder.AddProject<Projects.DatabaseMigrations_MigrationService>("migration")
    .WithReference(db1)
    .WaitFor(db1);

builder.AddProject<Projects.DatabaseMigrations_ApiService>("api")
    .WithReference(db1)
    .WaitForCompletion(migrationService);

builder.Build().Run();
