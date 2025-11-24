using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<MyDb1Context>("db1");

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
        entries
    };
});

app.MapDefaultEndpoints();

app.Run();
