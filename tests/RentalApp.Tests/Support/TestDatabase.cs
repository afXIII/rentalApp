using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;

namespace RentalApp.Tests.Support;

// A real SQLite database that lives in memory for as long as this object does.
// Each test gets its own. CreateContext() returns a fresh context on the same data,
// which is how two concurrent requests are simulated.
public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public DbContextOptions<AppDbContext> Options { get; }

    private TestDatabase()
    {
        Options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
    }

    public static async Task<TestDatabase> CreateAsync()
    {
        var database = new TestDatabase();
        await database.connection.OpenAsync();
        await using var db = database.CreateContext();
        await db.Database.EnsureCreatedAsync();
        return database;
    }

    public SqliteConnection Connection => connection;

    public AppDbContext CreateContext() => new(Options);

    // A property with one unit, plus the users needed to apply for and review it.
    public async Task<Scenario> SeedScenarioAsync()
    {
        await using var db = CreateContext();
        var applicant = new ApplicationUser { UserName = "a@test", Email = "a@test", FullName = "Ann Applicant" };
        var manager = new ApplicationUser { UserName = "m@test", Email = "m@test", FullName = "Max Manager" };
        var otherManager = new ApplicationUser { UserName = "m2@test", Email = "m2@test", FullName = "Mia Manager" };
        var unit = new Unit
        {
            UnitNumber = "101",
            Bedrooms = 2,
            MonthlyRent = 1500m,
            UnitType = new UnitType { Name = "Apartment" },
            Property = new Property
            {
                Name = "Maple Court", AddressLine = "1 Maple St", City = "Albany", State = "NY", PostalCode = "12207",
            },
        };
        db.AddRange(applicant, manager, otherManager, unit);
        await db.SaveChangesAsync();
        return new Scenario(unit.Id, applicant.Id, manager.Id, otherManager.Id);
    }

    public ValueTask DisposeAsync() => connection.DisposeAsync();
}

public record Scenario(int UnitId, string ApplicantId, string ManagerId, string OtherManagerId);
