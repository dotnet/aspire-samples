// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContextPool<MyDb1Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("db1"), sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("DatabaseMigrations.ApiModel");
        // Workround for https://github.com/dotnet/aspire/issues/1023
        sqlOptions.ExecutionStrategy(c => new RetryingSqlServerRetryingExecutionStrategy(c));
    }));
builder.EnrichSqlServerDbContext<MyDb1Context>(settings =>
    // Disable Aspire default retries as we're using a custom execution strategy
    settings.Retry = false);

var app = builder.Build();

app.MapGet("/", async (MyDb1Context context) =>
{
    var entry = new Entry();
    await context.Entries.AddAsync(entry);
    await context.SaveChangesAsync();

    var entries = await context.Entries.ToListAsync();

    return new
    {
        totalEntries = entries.Count,
        entries = entries
    };
});

app.MapDefaultEndpoints();

app.Run();
