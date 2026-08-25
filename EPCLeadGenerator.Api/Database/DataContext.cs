using Microsoft.EntityFrameworkCore;

namespace EPCLeadGenerator.Api.Database;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<LSOADeprivation> LSOADeprivation { get; set; }
    public DbSet<Postcode> Postcodes { get; set; }
    public DbSet<EPCAssessment> EPCAssessments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
