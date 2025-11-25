using Microsoft.EntityFrameworkCore;

public class MyDb1Context(DbContextOptions<MyDb1Context> options) : DbContext(options)
{
    public DbSet<Entry> Entries { get; set; }
}

public class Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
