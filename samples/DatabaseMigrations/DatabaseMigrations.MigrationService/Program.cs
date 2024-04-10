// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DatabaseMigrations.MigrationService;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ApiDbInitializer>();

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(ApiDbInitializer.ActivitySourceName));

builder.Services.AddDbContextPool<MyDb1Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("db1"), sqlOptions =>
    {
        // Workround for https://github.com/dotnet/aspire/issues/1023
        sqlOptions.ExecutionStrategy(c => new RetryingSqlServerRetryingExecutionStrategy(c));
    }));
builder.EnrichSqlServerDbContext<MyDb1Context>(settings =>
    // Disable Aspire default retries as we're using a custom execution strategy
    settings.Retry = false);

var app = builder.Build();

app.Run();
