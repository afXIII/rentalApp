namespace RentalApp.Web.Domain;

// One row per status change. Feeds the "who, when, comment" history shown to property managers.
public class ApplicationStatusHistory
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication RentalApplication { get; set; } = null!;

    // Null for the very first row (creation of the Draft).
    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }

    public required string ChangedById { get; set; }
    public ApplicationUser ChangedBy { get; set; } = null!;

    public DateTime ChangedAt { get; set; }
    public string? Comment { get; set; }
}
