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

    public bool Covers(DateOnly date) => StartDate <= date && date <= EndDate;

    // A twelve-month term ends the day before the anniversary: starting 2026-01-15 ends 2027-01-14.
    public static DateOnly EndDateFor(DateOnly startDate)
    {
        var anniversary = startDate.AddMonths(TermMonths);

        // AddMonths clamps Feb 29 to Feb 28. The anniversary of a leap day is Mar 1 in a common year.
        if (anniversary.Day != startDate.Day)
        {
            anniversary = anniversary.AddDays(1);
        }

        return anniversary.AddDays(-1);
    }
}
