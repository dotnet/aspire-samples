using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listly.Database;

public class ListlyDbContext(DbContextOptions<ListlyDbContext> options) : DbContext(options)
{

    private static readonly Func<ListlyDbContext, IAsyncEnumerable<ListItem>> s_getListItems =
        EF.CompileAsyncQuery((ListlyDbContext context) =>
            context.ListItems.AsNoTracking()
                .Where(x => !x.Bought));
    
    public Task<List<ListItem>> GetListItemsCompiledAsync()
    {
        return ToListAsync(s_getListItems(this));
    }

    public Task AddListItem(ListItem item)
    {
        this.ListItems.AddAsync(item, default);
        return SaveChangesAsync();
    }

    public async Task SetBoughtTrue(Guid itemId)
    {
        var item = await this.ListItems.FindAsync(itemId);
        item.Bought = true;
        this.ListItems.Update(item);
        await SaveChangesAsync();
    }
    
    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> asyncEnumerable)
    {
        var results = new List<T>();
        await foreach (var value in asyncEnumerable)
        {
            results.Add(value);
        }

        return results;
    }
    
    public DbSet<ListItem> ListItems => Set<ListItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        DefineListItem(builder.Entity<ListItem>());
    }

    private static void DefineListItem(EntityTypeBuilder<ListItem> builder)
    {
        builder.ToTable("ListItem");

        builder.HasKey(li => li.Id);
        
        builder.Property(li => li.Id)
            .IsRequired();

        builder.Property(li => li.Content)
            .IsRequired();
        
        builder.Property(li => li.Added)
            .IsRequired();
        
        builder.Property(li => li.Bought)
            .IsRequired();


    }

}
public class ListItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Content { get; set; }
    public DateTime Added { get; set; } = DateTime.UtcNow;
    public bool Bought { get; set; } = false;
}
