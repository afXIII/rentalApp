using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;

namespace RentalApp.Tests.Data;

public class MoneyToCentsConverterTests
{
    // [Theory] + [InlineData] is a data provider: the test runs once per row.
    [Theory]
    [InlineData("1250.00", 125000)]
    [InlineData("1250.5", 125050)]
    [InlineData("0.01", 1)]
    [InlineData("999.995", 99999 + 1)] // midpoint rounds away from zero
    [InlineData("0", 0)]
    public void ToCents_converts_dollars_to_whole_cents(string dollars, long expectedCents)
    {
        Assert.Equal(expectedCents, MoneyToCentsConverter.ToCents(decimal.Parse(dollars)));
    }

    [Fact]
    public void ToDollars_converts_cents_back_to_decimal()
    {
        Assert.Equal(1250.50m, MoneyToCentsConverter.ToDollars(125050));
    }

    [Fact]
    public async Task Rent_is_stored_as_cents_and_read_back_as_dollars()
    {
        // A real SQLite database that lives in memory for as long as this connection is open.
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            db.Units.Add(new Unit
            {
                UnitNumber = "101",
                Bedrooms = 2,
                MonthlyRent = 1499.99m,
                UnitType = new UnitType { Name = "Apartment" },
                Property = new Property
                {
                    Name = "Maple Court", AddressLine = "1 Maple St", City = "Albany", State = "NY", PostalCode = "12207",
                },
            });
            await db.SaveChangesAsync();
        }

        // Look at the raw column, bypassing EF, to prove what is physically stored.
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT MonthlyRentCents FROM Units";
        Assert.Equal(149999L, await command.ExecuteScalarAsync());

        // A fresh context reads it back as dollars, and can filter and sort on it in SQL.
        await using (var db = new AppDbContext(options))
        {
            var unit = await db.Units
                .Where(u => u.MonthlyRent > 1000m)
                .OrderBy(u => u.MonthlyRent)
                .SingleAsync();
            Assert.Equal(1499.99m, unit.MonthlyRent);
        }
    }
}
