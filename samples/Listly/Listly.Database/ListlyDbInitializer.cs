using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Listly.Database;

internal sealed class ListlyDbInitializer(IServiceProvider serviceProvider, ILogger<ListlyDbInitializer> logger)
    : BackgroundService
{
    public const string ActivitySourceName = "Migrations";

    private readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ListlyDbContext>();

        await InitializeDatabaseAsync(dbContext, cancellationToken);
    }

    private async Task InitializeDatabaseAsync(ListlyDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var strategy = dbContext.Database.CreateExecutionStrategy();

        using var activity = _activitySource.StartActivity("Migrating Shopping List database", ActivityKind.Client);

        var sw = Stopwatch.StartNew();

        strategy.Execute(() => dbContext.Database.Migrate());

        await SeedAsync(dbContext, cancellationToken);

        logger.LogInformation("Database initialization completed after {ElapsedMilliseconds}ms",
            sw.ElapsedMilliseconds);
    }

    private async Task SeedAsync(ListlyDbContext dbContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding database");

        static List<ListItem> GetPreconfiguredItems()
        {
            return new List<ListItem>()
            {
                new()
                {
                    Content = "Bananas",
                }
            };
        }


        if (!dbContext.ListItems.Any())
        {
            var items = GetPreconfiguredItems();
            await dbContext.ListItems.AddRangeAsync(items, cancellationToken);

            logger.LogInformation("Seeding {ListItemCount} list items", items.Count);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}