using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Domain;

namespace RentalApp.Web.Data;

// Inheriting IdentityDbContext adds the Identity tables (AspNetUsers, AspNetRoles, ...)
// to the same database as our own tables.
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    // Each DbSet is a table and the starting point for queries: db.Units.Where(...)
    public DbSet<UnitType> UnitTypes => Set<UnitType>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<RentalApplication> RentalApplications => Set<RentalApplication>();
    public DbSet<Residence> Residences => Set<Residence>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistory => Set<ApplicationStatusHistory>();
    public DbSet<Lease> Leases => Set<Lease>();

    // Model-wide conventions, applied to every property of a type (and its nullable form).
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Must run first: it configures the Identity tables.
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().Property(u => u.FullName).HasMaxLength(100);

        // Finds every IEntityTypeConfiguration class in this project (Data/Configurations) and applies it.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
