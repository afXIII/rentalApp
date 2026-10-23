using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;
using RentalApp.Web.Models.Applications;

namespace RentalApp.Web.Services;

// Builds the application page view model with one projection query. Projection (rather than
// loading the entity) means columns the page must not show, such as ManagerNotes, are never selected.
public class ApplicationPageBuilder(AppDbContext db)
{
    // Returns null when the application does not exist or the user may not see it
    // (applicants only see their own; property managers see all).
    public async Task<ApplicationPageViewModel?> BuildAsync(int id, string userId, bool isManager, ApplicationStep? requestedStep)
    {
        var a = await db.RentalApplications
            .AsNoTracking()
            .Where(x => x.Id == id && (isManager || x.ApplicantId == userId))
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.ApplicantId,
                ApplicantAccountName = x.Applicant.FullName,
                x.CreatedAt,
                x.SubmittedAt,
                x.FullName,
                x.Phone,
                x.Email,
                x.CurrentAddress,
                ApplicantInfoSaved = x.ApplicantInfoSavedAt != null,
                ResidenceHistorySaved = x.ResidenceHistorySavedAt != null,
                ResidenceCount = x.Residences.Count,
                x.ClaimedById,
                ClaimedByName = x.ClaimedBy != null ? x.ClaimedBy.FullName : null,
                ReturnComment = x.History
                    .Where(h => h.ToStatus == ApplicationStatus.Returned)
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => h.Comment)
                    .FirstOrDefault(),
                LeaseStart = x.Lease != null ? (DateOnly?)x.Lease.StartDate : null,
                LeaseEnd = x.Lease != null ? (DateOnly?)x.Lease.EndDate : null,
                Unit = new UnitSummaryViewModel
                {
                    UnitId = x.Unit.Id,
                    UnitNumber = x.Unit.UnitNumber,
                    PropertyName = x.Unit.Property.Name,
                    Address = x.Unit.Property.AddressLine + ", " + x.Unit.Property.City + ", " + x.Unit.Property.State,
                    UnitTypeName = x.Unit.UnitType.Name,
                    Bedrooms = x.Unit.Bedrooms,
                    MonthlyRent = x.Unit.MonthlyRent,
                },
            })
            .SingleOrDefaultAsync();

        if (a is null)
        {
            return null;
        }

        var isOwner = !isManager && a.ApplicantId == userId;
        var canEdit = isOwner && a.Status.IsEditable();
        var claimedByMe = isManager && a.Status == ApplicationStatus.UnderReview && a.ClaimedById == userId;

        return new ApplicationPageViewModel
        {
            Id = a.Id,
            Status = a.Status,
            Step = requestedStep ?? DefaultStep(canEdit, a.ApplicantInfoSaved, a.ResidenceHistorySaved),
            CanEdit = canEdit,
            IsManager = isManager,
            Unit = a.Unit,
            ApplicantAccountName = a.ApplicantAccountName,
            CreatedAt = a.CreatedAt,
            SubmittedAt = a.SubmittedAt,
            ApplicantInfo = new ApplicantInfoSection
            {
                FullName = a.FullName, Phone = a.Phone, Email = a.Email, CurrentAddress = a.CurrentAddress,
            },
            ApplicantInfoSaved = a.ApplicantInfoSaved,
            ResidenceHistorySaved = a.ResidenceHistorySaved,
            ResidenceCount = a.ResidenceCount,
            ReturnComment = a.Status == ApplicationStatus.Returned ? a.ReturnComment : null,
            LeaseStart = a.LeaseStart,
            LeaseEnd = a.LeaseEnd,
            ClaimedByName = a.ClaimedByName,
            CanClaim = isManager && a.Status == ApplicationStatus.Submitted,
            CanRelease = claimedByMe,
            CanReview = claimedByMe,
            CanWithdraw = isOwner && !a.Status.IsTerminal(),
        };
    }

    // Editing resumes at the first unsaved section. Everyone else starts on the Summary.
    private static ApplicationStep DefaultStep(bool canEdit, bool applicantInfoSaved, bool residenceHistorySaved)
    {
        if (!canEdit)
        {
            return ApplicationStep.Summary;
        }

        if (!applicantInfoSaved)
        {
            return ApplicationStep.ApplicantInfo;
        }

        return residenceHistorySaved ? ApplicationStep.Summary : ApplicationStep.ResidenceHistory;
    }
}
