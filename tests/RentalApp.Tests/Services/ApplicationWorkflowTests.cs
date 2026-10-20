using Microsoft.EntityFrameworkCore;
using RentalApp.Tests.Support;
using RentalApp.Web.Domain;
using RentalApp.Web.Services;

namespace RentalApp.Tests.Services;

public class ApplicationWorkflowTests
{
    private static readonly FixedClock Clock = new();

    private static ApplicationWorkflow Workflow(Web.Data.AppDbContext db) => new(db, Clock);

    // Drives a new application to Submitted through the workflow, as the controllers do.
    private static async Task<int> SubmitApplicationAsync(TestDatabase database, Scenario s)
    {
        await using var db = database.CreateContext();
        var workflow = Workflow(db);
        var started = await workflow.StartAsync(s.UnitId, s.ApplicantId);
        var application = (await workflow.FindForUpdateAsync(started.Id))!;
        await workflow.SaveApplicantInfoAsync(application, s.ApplicantId, "Ann", "555", "ann@test", "1 Main St");
        application.Residences.Add(new Residence
        {
            Address = "9 Elm St", LandlordName = "Lee", LandlordPhone = "555-0199",
            MoveInDate = new DateOnly(2024, 1, 1), MoveOutDate = new DateOnly(2026, 9, 30),
        });
        await workflow.SaveResidenceHistoryAsync(application, s.ApplicantId);
        await workflow.SubmitAsync(application, s.ApplicantId);
        return application.Id;
    }

    [Fact]
    public async Task Start_prefills_from_the_account_and_reuses_an_open_application()
    {
        await using var database = await TestDatabase.CreateAsync();
        var s = await database.SeedScenarioAsync();
        await using var db = database.CreateContext();

        var first = await Workflow(db).StartAsync(s.UnitId, s.ApplicantId);
        var second = await Workflow(db).StartAsync(s.UnitId, s.ApplicantId);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Ann Applicant", first.FullName);
        Assert.Null(first.ApplicantInfoSavedAt);
        Assert.Equal(1, await db.RentalApplications.CountAsync());
    }

    [Fact]
    public async Task Start_is_rejected_for_a_leased_unit()
    {
        await using var database = await TestDatabase.CreateAsync();
        var s = await database.SeedScenarioAsync();
        var applicationId = await SubmitApplicationAsync(database, s);
        await using (var db = database.CreateContext())
        {
            var workflow = Workflow(db);
            var application = (await workflow.FindForUpdateAsync(applicationId))!;
            await workflow.ClaimAsync(application, s.ManagerId);
            await workflow.CompleteReviewAsync(application, s.ManagerId, ReviewOutcome.Approve, null);
        }

        await using var fresh = database.CreateContext();
        var other = new Web.Domain.ApplicationUser { UserName = "b@test", Email = "b@test", FullName = "Bob" };
        fresh.Users.Add(other);
        await fresh.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainRuleException>(() => Workflow(fresh).StartAsync(s.UnitId, other.Id));
    }

    [Fact]
    public async Task Approval_persists_the_lease_and_the_status_history()
    {
        await using var database = await TestDatabase.CreateAsync();
        var s = await database.SeedScenarioAsync();
        var applicationId = await SubmitApplicationAsync(database, s);

        await using (var db = database.CreateContext())
        {
            var workflow = Workflow(db);
            var application = (await workflow.FindForUpdateAsync(applicationId))!;
            await workflow.ClaimAsync(application, s.ManagerId);
            await workflow.CompleteReviewAsync(application, s.ManagerId, ReviewOutcome.Approve, "Welcome");
        }

        await using var check = database.CreateContext();
        var lease = await check.Leases.SingleAsync();
        Assert.Equal((s.UnitId, applicationId, FixedClock.Today), (lease.UnitId, lease.RentalApplicationId, lease.StartDate));
        var statuses = await check.ApplicationStatusHistory
            .Where(h => h.RentalApplicationId == applicationId)
            .OrderBy(h => h.Id)
            .Select(h => h.ToStatus)
            .ToListAsync();
        Assert.Equal(
            [ApplicationStatus.Draft, ApplicationStatus.Submitted, ApplicationStatus.UnderReview, ApplicationStatus.Approved],
            statuses);
    }

    [Fact]
    public async Task Two_concurrent_approvals_for_the_same_unit_issue_only_one_lease()
    {
        await using var database = await TestDatabase.CreateAsync();
        var s = await database.SeedScenarioAsync();

        // Two applicants submit for the same unit and two managers each claim one.
        var firstId = await SubmitApplicationAsync(database, s);
        string secondApplicantId;
        await using (var db = database.CreateContext())
        {
            var second = new Web.Domain.ApplicationUser { UserName = "b@test", Email = "b@test", FullName = "Bob" };
            db.Users.Add(second);
            await db.SaveChangesAsync();
            secondApplicantId = second.Id;
        }

        var secondId = await SubmitApplicationAsync(database, s with { ApplicantId = secondApplicantId });

        // Both managers load their application (and the unit) before either one saves.
        await using var managerA = database.CreateContext();
        await using var managerB = database.CreateContext();
        var workflowA = Workflow(managerA);
        var workflowB = Workflow(managerB);
        var applicationA = (await workflowA.FindForUpdateAsync(firstId))!;
        var applicationB = (await workflowB.FindForUpdateAsync(secondId))!;
        await workflowA.ClaimAsync(applicationA, s.ManagerId);
        await workflowB.ClaimAsync(applicationB, s.OtherManagerId);

        await workflowA.CompleteReviewAsync(applicationA, s.ManagerId, ReviewOutcome.Approve, null);
        var error = await Assert.ThrowsAsync<DomainRuleException>(() =>
            workflowB.CompleteReviewAsync(applicationB, s.OtherManagerId, ReviewOutcome.Approve, null));

        Assert.Contains("at the same time", error.Message);
        await using var check = database.CreateContext();
        Assert.Equal(1, await check.Leases.CountAsync());
        Assert.Equal(ApplicationStatus.UnderReview,
            await check.RentalApplications.Where(a => a.Id == secondId).Select(a => a.Status).SingleAsync());
    }

    [Fact]
    public async Task Other_open_applications_for_a_leased_unit_are_left_as_they_are()
    {
        await using var database = await TestDatabase.CreateAsync();
        var s = await database.SeedScenarioAsync();
        var firstId = await SubmitApplicationAsync(database, s);
        string otherApplicantId;
        await using (var db = database.CreateContext())
        {
            var other = new Web.Domain.ApplicationUser { UserName = "b@test", Email = "b@test", FullName = "Bob" };
            db.Users.Add(other);
            await db.SaveChangesAsync();
            otherApplicantId = other.Id;
        }

        var secondId = await SubmitApplicationAsync(database, s with { ApplicantId = otherApplicantId });

        await using (var db = database.CreateContext())
        {
            var workflow = Workflow(db);
            var application = (await workflow.FindForUpdateAsync(firstId))!;
            await workflow.ClaimAsync(application, s.ManagerId);
            await workflow.CompleteReviewAsync(application, s.ManagerId, ReviewOutcome.Approve, null);
        }

        await using (var db = database.CreateContext())
        {
            // The second application is untouched, and approving it later is refused.
            var workflow = Workflow(db);
            var second = (await workflow.FindForUpdateAsync(secondId))!;
            Assert.Equal(ApplicationStatus.Submitted, second.Status);
            await workflow.ClaimAsync(second, s.ManagerId);
            await Assert.ThrowsAsync<DomainRuleException>(() =>
                workflow.CompleteReviewAsync(second, s.ManagerId, ReviewOutcome.Approve, null));
        }
    }

    [Fact]
    public async Task Availability_filter_is_translated_to_sql()
    {
        await using var database = await TestDatabase.CreateAsync();
        var s = await database.SeedScenarioAsync();
        var today = FixedClock.Today;
        await using var db = database.CreateContext();
        var expiredApplication = Web.Domain.RentalApplication.Start(s.UnitId, s.ApplicantId, Applications.Now);
        db.RentalApplications.Add(expiredApplication);
        await db.SaveChangesAsync();
        db.Leases.Add(new Lease
        {
            UnitId = s.UnitId, RentalApplicationId = expiredApplication.Id, TenantId = s.ApplicantId,
            StartDate = today.AddYears(-2), EndDate = today.AddYears(-1),
        });
        await db.SaveChangesAsync();

        Assert.True(await db.Units.Where(Unit.IsAvailableOn(today)).AnyAsync());
        Assert.False(await db.Units.Where(Unit.IsAvailableOn(today.AddYears(-1))).AnyAsync());
    }
}
