using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;

namespace RentalApp.Web.Services;

// Application use cases. Loads what the domain needs, calls the domain method that
// enforces the rule, and saves. Rule violations surface as DomainRuleException.
public class ApplicationWorkflow(AppDbContext db, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // "Today" for lease terms is the calendar date where the business operates (server local time).
    public DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    // Everything a status change needs: residences for the Submit check, the unit's leases for the lease check.
    public Task<RentalApplication?> FindForUpdateAsync(int id) =>
        db.RentalApplications
            .Include(a => a.Residences)
            .Include(a => a.Unit).ThenInclude(u => u.Leases)
            .SingleOrDefaultAsync(a => a.Id == id);

    // Returns the applicant's open application for the unit if one exists, so a double click
    // on "Apply" does not create two drafts.
    public async Task<RentalApplication> StartAsync(int unitId, string applicantId)
    {
        var existing = await db.RentalApplications
            .Where(a => a.UnitId == unitId && a.ApplicantId == applicantId)
            .Where(a => a.Status != ApplicationStatus.Approved
                        && a.Status != ApplicationStatus.Denied
                        && a.Status != ApplicationStatus.Withdrawn)
            .FirstOrDefaultAsync();
        if (existing is not null)
        {
            return existing;
        }

        var unitIsAvailable = await db.Units
            .Where(u => u.Id == unitId)
            .Where(Unit.IsAvailableOn(Today))
            .AnyAsync();
        if (!unitIsAvailable)
        {
            throw new DomainRuleException("This unit is not available.");
        }

        var application = RentalApplication.Start(unitId, applicantId, Now);

        // Pre-fill section 1 from the account so the applicant has less to type. Not marked as saved.
        var user = await db.Users.SingleAsync(u => u.Id == applicantId);
        application.PrefillApplicantInfo(user.FullName, user.PhoneNumber, user.Email);

        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();
        return application;
    }

    public async Task SaveApplicantInfoAsync(RentalApplication application, string userId,
        string fullName, string phone, string email, string currentAddress)
    {
        application.SaveApplicantInfo(userId, fullName, phone, email, currentAddress, Now);
        await SaveAsync();
    }

    public async Task SaveResidenceHistoryAsync(RentalApplication application, string userId)
    {
        application.SaveResidenceHistory(userId, Now);
        await SaveAsync();
    }

    public async Task SubmitAsync(RentalApplication application, string userId)
    {
        application.Submit(userId, application.Unit, Today, Now);
        await SaveAsync();
    }

    public async Task WithdrawAsync(RentalApplication application, string userId)
    {
        application.Withdraw(userId, Now);
        await SaveAsync();
    }

    public async Task ClaimAsync(RentalApplication application, string managerId)
    {
        application.Claim(managerId, Now);
        await SaveAsync();
    }

    public async Task ReleaseAsync(RentalApplication application, string managerId)
    {
        application.Release(managerId, Now);
        await SaveAsync();
    }

    public async Task CompleteReviewAsync(RentalApplication application, string managerId, ReviewOutcome outcome, string? comment)
    {
        switch (outcome)
        {
            case ReviewOutcome.Approve:
                application.Approve(managerId, application.Unit, Today, Now, comment);
                break;
            case ReviewOutcome.Return:
                application.ReturnToApplicant(managerId, comment, Now);
                break;
            case ReviewOutcome.Deny:
                application.Deny(managerId, comment, Now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        await SaveAsync();
    }

    // Saves, turning an optimistic concurrency failure into a message the user can act on.
    // This is what stops two approvals for the same unit from both issuing a lease:
    // the second one's UPDATE of Unit.LeaseVersion matches zero rows.
    private async Task SaveAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainRuleException(
                "Someone else changed this application or unit at the same time. Reload the page and try again.");
        }
    }
}
