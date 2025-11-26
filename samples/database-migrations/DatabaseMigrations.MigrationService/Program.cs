using DatabaseMigrations.MigrationService;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<ApiDbInitializer>();

builder.AddServiceDefaults();

builder.Services.AddDbContextPool<MyDb1Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("db1"), sqlOptions =>
        sqlOptions.MigrationsAssembly("DatabaseMigrations.MigrationService")
    ));
builder.EnrichSqlServerDbContext<MyDb1Context>();

var app = builder.Build();

app.Run();
