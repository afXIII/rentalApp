using RentalApp.Tests.Support;
using RentalApp.Web.Domain;
using RentalApp.Web.Services;

namespace RentalApp.Tests.Services;

public class ApplicationListServiceTests
{
    // Two properties, two applicants. Ann has a Draft at Maple and a Withdrawn at Oak; Bob has a Draft at Oak.
    private static async Task<(TestDatabase Database, string Ann, string Bob, int MapleId, int OakId)> SeedAsync()
    {
        var database = await TestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var ann = new ApplicationUser { UserName = "ann@test", Email = "ann@test", FullName = "Ann" };
        var bob = new ApplicationUser { UserName = "bob@test", Email = "bob@test", FullName = "Bob" };
        var type = new UnitType { Name = "Apartment" };
        var maple = new Property { Name = "Maple", AddressLine = "1", City = "A", State = "NY", PostalCode = "1" };
        var oak = new Property { Name = "Oak", AddressLine = "2", City = "B", State = "NY", PostalCode = "2" };
        var mapleUnit = new Unit { UnitNumber = "101", Bedrooms = 1, MonthlyRent = 1000m, UnitType = type, Property = maple };
        var oakUnit = new Unit { UnitNumber = "201", Bedrooms = 2, MonthlyRent = 2000m, UnitType = type, Property = oak };
        db.AddRange(ann, bob, mapleUnit, oakUnit);
        await db.SaveChangesAsync();

        var withdrawn = RentalApplication.Start(oakUnit.Id, ann.Id, Applications.Now.AddDays(-2));
        withdrawn.Withdraw(ann.Id, Applications.Now);
        db.RentalApplications.AddRange(
            RentalApplication.Start(mapleUnit.Id, ann.Id, Applications.Now.AddDays(-3)),
            withdrawn,
            RentalApplication.Start(oakUnit.Id, bob.Id, Applications.Now.AddDays(-1)));
        await db.SaveChangesAsync();
        return (database, ann.Id, bob.Id, maple.Id, oak.Id);
    }

    [Fact]
    public async Task Applicants_see_only_their_own_applications()
    {
        var (database, ann, _, _, _) = await SeedAsync();
        await using var _ = database;
        await using var db = database.CreateContext();

        var page = await new ApplicationListService(db).GetPageAsync(new ApplicationListRequest(), ann, isManager: false);

        Assert.Equal(2, page.Total);
        Assert.All(page.Rows, r => Assert.Equal("Ann", r.ApplicantName));
    }

    [Fact]
    public async Task Managers_see_all_applications()
    {
        var (database, _, bob, _, _) = await SeedAsync();
        await using var _ = database;
        await using var db = database.CreateContext();

        var page = await new ApplicationListService(db).GetPageAsync(new ApplicationListRequest(), bob, isManager: true);

        Assert.Equal(3, page.Total);
    }

    [Fact]
    public async Task Filters_by_status_and_property()
    {
        var (database, ann, _, _, oakId) = await SeedAsync();
        await using var _ = database;
        await using var db = database.CreateContext();
        var service = new ApplicationListService(db);

        var drafts = await service.GetPageAsync(new ApplicationListRequest { Status = ApplicationStatus.Draft }, ann, isManager: true);
        var oak = await service.GetPageAsync(new ApplicationListRequest { PropertyId = oakId }, ann, isManager: true);
        var oakDrafts = await service.GetPageAsync(
            new ApplicationListRequest { PropertyId = oakId, Status = ApplicationStatus.Draft }, ann, isManager: true);

        Assert.Equal(2, drafts.Total);
        Assert.Equal(2, oak.Total);
        Assert.Equal("Bob", Assert.Single(oakDrafts.Rows).ApplicantName);
    }

    [Fact]
    public async Task Pages_and_sorts_in_the_database_and_reports_the_filtered_total()
    {
        var (database, ann, _, _, _) = await SeedAsync();
        await using var _ = database;
        await using var db = database.CreateContext();
        var service = new ApplicationListService(db);

        var page1 = await service.GetPageAsync(new ApplicationListRequest { Sort = "created", Direction = "asc", PageSize = 2 }, ann, isManager: true);
        var page2 = await service.GetPageAsync(new ApplicationListRequest { Sort = "created", Direction = "asc", PageSize = 2, Page = 2 }, ann, isManager: true);

        Assert.Equal(3, page1.Total);
        Assert.Equal(["Maple", "Oak"], page1.Rows.Select(r => r.PropertyName));
        Assert.Equal("Bob", Assert.Single(page2.Rows).ApplicantName);
    }

    [Fact]
    public async Task Unknown_sort_columns_fall_back_and_page_size_is_capped()
    {
        var (database, ann, _, _, _) = await SeedAsync();
        await using var _ = database;
        await using var db = database.CreateContext();

        var page = await new ApplicationListService(db).GetPageAsync(
            new ApplicationListRequest { Sort = "PasswordHash", PageSize = 5000, Page = -3 }, ann, isManager: true);

        Assert.Equal(ApplicationListRequest.MaxPageSize, page.PageSize);
        Assert.Equal(1, page.Page);
        Assert.Equal(3, page.Rows.Count);
    }
}
