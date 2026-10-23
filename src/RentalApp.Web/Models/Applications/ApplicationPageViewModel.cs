using System.ComponentModel.DataAnnotations;
using RentalApp.Web.Domain;

namespace RentalApp.Web.Models.Applications;

// The sections of the application page, in order.
public enum ApplicationStep
{
    ApplicantInfo,
    ResidenceHistory,
    Summary,
}

// The buttons of the application form. The clicked button's value arrives as "command".
public static class ApplicationCommand
{
    public const string Continue = "continue";
    public const string Back = "back";
    public const string Submit = "submit";
}

// One view model drives the whole application page. Each section renders through its own
// partial view (_ApplicantInfoSection) or view component (ResidenceHistory), which reads
// IsEditing(section) to decide between inputs and read-only text.
public class ApplicationPageViewModel
{
    public int Id { get; init; }
    public ApplicationStatus Status { get; init; }
    public ApplicationStep Step { get; set; }

    // Server-side decision: the signed-in user owns the application and its status is Draft or Returned.
    public bool CanEdit { get; init; }
    public bool IsManager { get; init; }

    public required UnitSummaryViewModel Unit { get; init; }
    public required string ApplicantAccountName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }

    // Section 1. Bound from the form when the applicant continues from that section.
    public ApplicantInfoSection ApplicantInfo { get; set; } = new();
    public bool ApplicantInfoSaved { get; init; }

    // Section 2 is rendered by the ResidenceHistory view component, which loads its own rows.
    public bool ResidenceHistorySaved { get; init; }
    public int ResidenceCount { get; init; }

    // The comment of the latest Return, shown to the applicant so they know what to fix.
    public string? ReturnComment { get; init; }

    public DateOnly? LeaseStart { get; init; }
    public DateOnly? LeaseEnd { get; init; }

    // Review queue (property managers).
    public string? ClaimedByName { get; init; }
    public bool CanClaim { get; init; }
    public bool CanRelease { get; init; }
    public bool CanReview { get; init; }

    public bool CanWithdraw { get; init; }

    public bool CanSubmit => CanEdit && ApplicantInfoSaved && ResidenceHistorySaved && ResidenceCount > 0;

    public bool IsEditing(ApplicationStep section) => CanEdit && Step == section;

    public bool IsSaved(ApplicationStep section) => section switch
    {
        ApplicationStep.ApplicantInfo => ApplicantInfoSaved,
        ApplicationStep.ResidenceHistory => ResidenceHistorySaved,
        _ => false,
    };
}

public class UnitSummaryViewModel
{
    public int UnitId { get; init; }
    public required string UnitNumber { get; init; }
    public required string PropertyName { get; init; }
    public required string Address { get; init; }
    public required string UnitTypeName { get; init; }
    public int Bedrooms { get; init; }
    public decimal MonthlyRent { get; init; }
}

public class ApplicantInfoSection
{
    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string? FullName { get; set; }

    [Required, Phone, StringLength(30)]
    public string? Phone { get; set; }

    [Required, EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [Required, StringLength(300)]
    [Display(Name = "Current address")]
    public string? CurrentAddress { get; set; }
}
