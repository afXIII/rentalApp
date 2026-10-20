namespace RentalApp.Web.Domain;

// The application aggregate. Every status change goes through a method here, so the
// rules (who may do what, in which status) live in one place and are unit tested
// without a database. Controllers and services never assign Status directly.
public class RentalApplication
{
    public const int CommentMaxLength = 1000;

    public int Id { get; set; }
    public ApplicationStatus Status { get; private set; } = ApplicationStatus.Draft;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    // Identity user ids are strings (GUIDs), not ints.
    public required string ApplicantId { get; set; }
    public ApplicationUser Applicant { get; set; } = null!;

    // Section 1: Applicant Information. Nullable because a Draft starts empty.
    public string? FullName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? CurrentAddress { get; private set; }

    // Section 2: Residence History.
    public List<Residence> Residences { get; set; } = [];

    // Set when a section passes validation and is saved. Submit requires both.
    public DateTime? ApplicantInfoSavedAt { get; private set; }
    public DateTime? ResidenceHistorySavedAt { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; private set; }

    // Review queue: the property manager who claimed the application (status UnderReview).
    public string? ClaimedById { get; private set; }
    public ApplicationUser? ClaimedBy { get; set; }
    public DateTime? ClaimedAt { get; private set; }

    // Internal to property managers. Never projected into anything an applicant receives.
    public string? ManagerNotes { get; set; }

    // Optimistic concurrency token. EF adds "WHERE Version = <value read>" to every UPDATE
    // of this row, so two requests acting on the same stale copy cannot both win.
    public int Version { get; private set; }

    public List<ApplicationStatusHistory> History { get; set; } = [];
    public Lease? Lease { get; set; }

    public bool IsOwnedBy(string userId) => ApplicantId == userId;

    public bool CanBeEditedBy(string userId) => IsOwnedBy(userId) && Status.IsEditable();

    public bool AllSectionsSaved => ApplicantInfoSavedAt is not null && ResidenceHistorySavedAt is not null;

    public bool IsClaimedBy(string managerId) => Status == ApplicationStatus.UnderReview && ClaimedById == managerId;

    public static RentalApplication Start(int unitId, string applicantId, DateTime now)
    {
        var application = new RentalApplication { UnitId = unitId, ApplicantId = applicantId, CreatedAt = now };
        application.History.Add(new ApplicationStatusHistory
        {
            FromStatus = null,
            ToStatus = ApplicationStatus.Draft,
            ChangedById = applicantId,
            ChangedAt = now,
        });
        return application;
    }

    // ---- Applicant actions ----

    // Copies account details into a new draft. Does not mark the section as saved.
    public void PrefillApplicantInfo(string? fullName, string? phone, string? email)
    {
        FullName ??= fullName;
        Phone ??= phone;
        Email ??= email;
    }

    public void SaveApplicantInfo(string userId, string fullName, string phone, string email, string currentAddress, DateTime now)
    {
        EnsureEditableBy(userId);
        FullName = fullName;
        Phone = phone;
        Email = email;
        CurrentAddress = currentAddress;
        ApplicantInfoSavedAt = now;
    }

    public void SaveResidenceHistory(string userId, DateTime now)
    {
        EnsureEditableBy(userId);
        if (Residences.Count == 0)
        {
            throw new DomainRuleException("Add at least one prior residence.");
        }

        ResidenceHistorySavedAt = now;
    }

    public void EnsureEditableBy(string userId)
    {
        if (!IsOwnedBy(userId))
        {
            throw new DomainRuleException("This is not your application.");
        }

        if (!Status.IsEditable())
        {
            throw new DomainRuleException($"An application that is {Status.DisplayName()} cannot be changed.");
        }
    }

    // `unit` must be loaded with its Leases, and this application with its Residences.
    public void Submit(string userId, Unit unit, DateOnly today, DateTime now)
    {
        EnsureEditableBy(userId);
        if (!AllSectionsSaved)
        {
            throw new DomainRuleException("Save both sections before submitting.");
        }

        // Residences can be removed after the section was saved, so check again.
        if (Residences.Count == 0)
        {
            throw new DomainRuleException("Add at least one prior residence.");
        }

        EnsureUnitHasNoActiveLease(unit, today);
        SubmittedAt = now;
        TransitionTo(ApplicationStatus.Submitted, userId, now, comment: null);
    }

    public void Withdraw(string userId, DateTime now)
    {
        if (!IsOwnedBy(userId))
        {
            throw new DomainRuleException("This is not your application.");
        }

        EnsureNotTerminal();
        ClearClaim();
        TransitionTo(ApplicationStatus.Withdrawn, userId, now, comment: null);
    }

    // ---- Property manager actions ----

    public void Claim(string managerId, DateTime now)
    {
        if (Status != ApplicationStatus.Submitted)
        {
            throw new DomainRuleException("Only a submitted application can be claimed for review.");
        }

        ClaimedById = managerId;
        ClaimedAt = now;
        TransitionTo(ApplicationStatus.UnderReview, managerId, now, comment: null);
    }

    public void Release(string managerId, DateTime now)
    {
        EnsureClaimedBy(managerId);
        ClearClaim();
        TransitionTo(ApplicationStatus.Submitted, managerId, now, comment: null);
    }

    // `unit` must be loaded with its Leases. Returns the new lease, already attached to this application.
    public Lease Approve(string managerId, Unit unit, DateOnly today, DateTime now, string? comment)
    {
        EnsureClaimedBy(managerId);
        EnsureUnitHasNoActiveLease(unit, today);

        var lease = new Lease
        {
            UnitId = UnitId,
            TenantId = ApplicantId,
            StartDate = today,
            EndDate = Lease.EndDateFor(today),
        };
        Lease = lease;

        // Changing the token makes a concurrent approval for the same unit fail on save.
        unit.LeaseVersion++;

        ClearClaim();
        TransitionTo(ApplicationStatus.Approved, managerId, now, comment);
        return lease;
    }

    public void ReturnToApplicant(string managerId, string? comment, DateTime now)
    {
        EnsureClaimedBy(managerId);
        RequireComment(comment, "Return");
        ClearClaim();
        TransitionTo(ApplicationStatus.Returned, managerId, now, comment);
    }

    public void Deny(string managerId, string? comment, DateTime now)
    {
        EnsureClaimedBy(managerId);
        RequireComment(comment, "Deny");
        ClearClaim();
        TransitionTo(ApplicationStatus.Denied, managerId, now, comment);
    }

    // ---- Helpers ----

    private void EnsureClaimedBy(string managerId)
    {
        if (Status != ApplicationStatus.UnderReview)
        {
            throw new DomainRuleException("The application must be claimed for review first.");
        }

        if (ClaimedById != managerId)
        {
            throw new DomainRuleException("Another property manager has claimed this application.");
        }
    }

    private void EnsureNotTerminal()
    {
        if (Status.IsTerminal())
        {
            throw new DomainRuleException($"The application is already {Status.DisplayName()}.");
        }
    }

    private static void EnsureUnitHasNoActiveLease(Unit unit, DateOnly today)
    {
        if (unit.Leases.Any(l => l.Covers(today)))
        {
            throw new DomainRuleException($"Unit {unit.UnitNumber} already has an active lease.");
        }
    }

    private static void RequireComment(string? comment, string outcome)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainRuleException($"A comment is required to {outcome.ToLowerInvariant()} an application.");
        }
    }

    private void ClearClaim()
    {
        ClaimedById = null;
        ClaimedAt = null;
    }

    private void TransitionTo(ApplicationStatus to, string userId, DateTime now, string? comment)
    {
        History.Add(new ApplicationStatusHistory
        {
            FromStatus = Status,
            ToStatus = to,
            ChangedById = userId,
            ChangedAt = now,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
        });
        Status = to;
        Version++;
    }
}
