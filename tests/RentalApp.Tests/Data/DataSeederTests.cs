using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentalApp.Tests.Support;
using RentalApp.Web.Data;
using RentalApp.Web.Data.Seed;
using RentalApp.Web.Domain;

namespace RentalApp.Tests.Data;

public class DataSeederTests
{
    // Builds just enough of the app's container for the seeder: EF, Identity, and the clock.
    private static ServiceProvider BuildServices(TestDatabase database)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(database.Connection));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();
        services.AddSingleton<TimeProvider>(new FixedClock());
        services.AddScoped<DataSeeder>();
        return services.BuildServiceProvider();
    }

    private static async Task SeedAsync(ServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await DatabaseInitializer.SeedRolesAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
        await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
    }

    [Fact]
    public async Task Seeding_twice_does_not_duplicate_anything()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var services = BuildServices(database);

        await SeedAsync(services);
        var first = await CountsAsync(database);
        await SeedAsync(services);
        var second = await CountsAsync(database);

        Assert.Equal(first, second);
        Assert.All(new[] { first.Users, first.UnitTypes, first.Properties, first.Units, first.Applications }, n => Assert.True(n > 0));
    }

    [Fact]
    public async Task Seed_covers_every_status_with_consistent_leases()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var services = BuildServices(database);
        await SeedAsync(services);
        await using var db = database.CreateContext();

        var statuses = await db.RentalApplications.Select(a => a.Status).Distinct().ToListAsync();
        Assert.Equal(Enum.GetValues<ApplicationStatus>().OrderBy(s => s), statuses.OrderBy(s => s));

        // Every approval has a lease, and nothing else does.
        var approved = await db.RentalApplications.CountAsync(a => a.Status == ApplicationStatus.Approved);
        Assert.Equal(approved, await db.Leases.CountAsync());

        // The lookup has an inactive value that is still in use.
        Assert.True(await db.Units.AnyAsync(u => !u.UnitType.IsActive));

        // The demo applicant has an application in every status.
        var demo = await db.Users.SingleAsync(u => u.Email == DataSeeder.DemoApplicantEmail);
        var demoStatuses = await db.RentalApplications.Where(a => a.ApplicantId == demo.Id).Select(a => a.Status).Distinct().CountAsync();
        Assert.Equal(Enum.GetValues<ApplicationStatus>().Length, demoStatuses);
    }

    private static async Task<SeedCounts> CountsAsync(TestDatabase database)
    {
        await using var db = database.CreateContext();
        return new SeedCounts(
            await db.Users.CountAsync(),
            await db.UnitTypes.CountAsync(),
            await db.Properties.CountAsync(),
            await db.Units.CountAsync(),
            await db.RentalApplications.CountAsync(),
            await db.Residences.CountAsync(),
            await db.Leases.CountAsync(),
            await db.ApplicationStatusHistory.CountAsync());
    }

    private record SeedCounts(
        int Users, int UnitTypes, int Properties, int Units, int Applications, int Residences, int Leases, int History);
}
