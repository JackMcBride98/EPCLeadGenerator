using Microsoft.EntityFrameworkCore;

namespace JackTemplate.Api.Database;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add on model creating logic here, e.g. configuring relationships, indexes, etc.
    }
}
