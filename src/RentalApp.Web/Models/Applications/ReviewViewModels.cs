using System.ComponentModel.DataAnnotations;
using RentalApp.Web.Domain;

namespace RentalApp.Web.Models.Applications;

public class ReviewFormViewModel : IValidatableObject
{
    public int ApplicationId { get; set; }

    [Required(ErrorMessage = "Choose an outcome.")]
    public ReviewOutcome? Outcome { get; set; }

    [StringLength(RentalApplication.CommentMaxLength)]
    public string? Comment { get; set; }

    // The same rule is enforced again in the domain (RentalApplication.ReturnToApplicant / Deny).
    // Here it puts the message next to the field.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Outcome is ReviewOutcome.Return or ReviewOutcome.Deny && string.IsNullOrWhiteSpace(Comment))
        {
            yield return new ValidationResult($"A comment is required to {Outcome.ToString()!.ToLowerInvariant()}.", [nameof(Comment)]);
        }
    }
}

public class ManagerNotesViewModel
{
    public int ApplicationId { get; init; }
    public string? Notes { get; init; }
}

public class ManagerNotesFormViewModel
{
    public int ApplicationId { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }
}

public class StatusHistoryRowViewModel
{
    public ApplicationStatus? FromStatus { get; init; }
    public ApplicationStatus ToStatus { get; init; }
    public required string ChangedByName { get; init; }
    public DateTime ChangedAt { get; init; }
    public string? Comment { get; init; }
}
