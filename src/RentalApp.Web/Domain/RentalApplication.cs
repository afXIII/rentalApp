namespace RentalApp.Web.Domain;

public class RentalApplication
{
    public int Id { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    // Identity user ids are strings (GUIDs), not ints.
    public required string ApplicantId { get; set; }
    public ApplicationUser Applicant { get; set; } = null!;

    // Section 1: Applicant Information. Nullable because a Draft starts empty.
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? CurrentAddress { get; set; }

    // Section 2: Residence History.
    public List<Residence> Residences { get; set; } = [];

    // Set when a section passes validation and is saved. Submit requires both.
    public DateTime? ApplicantInfoSavedAt { get; set; }
    public DateTime? ResidenceHistorySavedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public List<ApplicationStatusHistory> History { get; set; } = [];
    public Lease? Lease { get; set; }
}
