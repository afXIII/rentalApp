using System.Linq.Expressions;

namespace RentalApp.Web.Domain;

public class Unit
{
    public int Id { get; set; }
    public required string UnitNumber { get; set; }
    public int Bedrooms { get; set; }

    // Dollars in C#. Stored as integer cents by a value converter (see Data/Configurations/PropertyAndUnitConfiguration.cs).
    public decimal MonthlyRent { get; set; }

    // A "belongsTo" is two members: the foreign key column and the navigation property.
    // "= null!" tells the compiler "EF will populate this, stop warning me that it could be null".
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public int UnitTypeId { get; set; }
    public UnitType UnitType { get; set; } = null!;

    public List<Lease> Leases { get; set; } = [];
    public List<RentalApplication> Applications { get; set; } = [];

    // Optimistic concurrency token, bumped every time a lease is issued for this unit.
    // Two approvals racing for the same unit both read the same value; the second
    // save fails with DbUpdateConcurrencyException instead of creating a second lease.
    public int LeaseVersion { get; set; }

    // A unit is available when no lease term covers the given day.
    // An expression (not a method) so EF can translate it to SQL inside Where().
    public static Expression<Func<Unit, bool>> IsAvailableOn(DateOnly date) =>
        u => !u.Leases.Any(l => l.StartDate <= date && l.EndDate >= date);
}
