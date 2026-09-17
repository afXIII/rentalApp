namespace RentalApp.Web.Domain;

public class Lease
{
    public const int TermMonths = 12;

    public int Id { get; set; }

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    // One lease per approved application (unique index in LeaseConfiguration).
    public int RentalApplicationId { get; set; }
    public RentalApplication RentalApplication { get; set; } = null!;

    public required string TenantId { get; set; }
    public ApplicationUser Tenant { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    // Last day of the lease, inclusive.
    public DateOnly EndDate { get; set; }
}
