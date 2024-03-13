// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var db1 = builder.AddSqlServer("sql1").AddDatabase("db1");

builder.AddProject<Projects.DatabaseMigrations_ApiService>("api")
       .WithReference(db1);

builder.AddProject<Projects.DatabaseMigrations_MigrationService>("migration")
       .WithReference(db1);

builder.Build().Run();
