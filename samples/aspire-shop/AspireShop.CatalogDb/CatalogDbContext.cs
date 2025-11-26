using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireShop.CatalogDb;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    // https://learn.microsoft.com/ef/core/performance/advanced-performance-topics#compiled-queries

    private static readonly Func<CatalogDbContext, int?, int?, int, IAsyncEnumerable<CatalogItem>> GetCatalogItemsAfterQuery =
        EF.CompileAsyncQuery((CatalogDbContext context, int? catalogBrandId, int? after, int pageSize) =>
           context.CatalogItems.AsNoTracking()
               .Where(ci => catalogBrandId == null || ci.CatalogBrandId == catalogBrandId)
               .Where(ci => after == null || ci.Id >= after)
               .OrderBy(ci => ci.Id)
               .Take(pageSize + 1));

    private static readonly Func<CatalogDbContext, int?, int, int, IAsyncEnumerable<CatalogItem>> GetCatalogItemsBeforeQuery =
        EF.CompileAsyncQuery((CatalogDbContext context, int? catalogBrandId, int before, int pageSize) =>
           context.CatalogItems.AsNoTracking()
               .Where(ci => catalogBrandId == null || ci.CatalogBrandId == catalogBrandId)
               .Where(ci => ci.Id <= before)
               .OrderByDescending(ci => ci.Id)
               .Take(pageSize + 1)
               .OrderBy(ci => ci.Id)
               .AsQueryable());

    public Task<List<CatalogItem>> GetCatalogItemsCompiledAsync(int? catalogBrandId, int? before, int? after, int pageSize)
    {
        // Using keyset pagination: https://learn.microsoft.com/ef/core/querying/pagination#keyset-pagination
        return ToListAsync(before is null
            // Paging forward
            ? GetCatalogItemsAfterQuery(this, catalogBrandId, after, pageSize)
            // Paging backward
            : GetCatalogItemsBeforeQuery(this, catalogBrandId, before.Value, pageSize));
    }

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();

    public DbSet<CatalogBrand> CatalogBrands => Set<CatalogBrand>();
    
    public DbSet<CatalogType> CatalogTypes => Set<CatalogType>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        DefineCatalogBrand(builder.Entity<CatalogBrand>());

        DefineCatalogItem(builder.Entity<CatalogItem>());

        DefineCatalogType(builder.Entity<CatalogType>());
    }

    private static void DefineCatalogType(EntityTypeBuilder<CatalogType> builder)
    {
        builder.ToTable("CatalogType");

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Id)
            .UseHiLo("catalog_type_hilo")
            .IsRequired();

        builder.Property(cb => cb.Type)
            .IsRequired()
            .HasMaxLength(100);
    }

    private static void DefineCatalogItem(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("Catalog");

        builder.Property(ci => ci.Id)
                    .UseHiLo("catalog_hilo")
                    .IsRequired();

        builder.Property(ci => ci.Name)
            .IsRequired(true)
            .HasMaxLength(50);

        builder.Property(ci => ci.Price)
            .IsRequired(true);

        builder.Property(ci => ci.PictureFileName)
            .IsRequired(false);

        builder.Ignore(ci => ci.PictureUri);

        builder.HasOne(ci => ci.CatalogBrand)
            .WithMany()
            .HasForeignKey(ci => ci.CatalogBrandId);

        builder.HasOne(ci => ci.CatalogType)
            .WithMany()
            .HasForeignKey(ci => ci.CatalogTypeId);
    }

    private static void DefineCatalogBrand(EntityTypeBuilder<CatalogBrand> builder)
    {
        builder.ToTable("CatalogBrand");
        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Id)
            .UseHiLo("catalog_brand_hilo")
            .IsRequired();

        builder.Property(cb => cb.Brand)
            .IsRequired()
            .HasMaxLength(100);
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
}
