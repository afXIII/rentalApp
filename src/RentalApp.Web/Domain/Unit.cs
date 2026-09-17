namespace RentalApp.Web.Domain;

public class Unit
{
    public int Id { get; set; }
    public required string UnitNumber { get; set; }
    public int Bedrooms { get; set; }

    // Dollars in C#. Stored as integer cents by a value converter (see Data/Configurations/UnitConfiguration.cs).
    public decimal MonthlyRent { get; set; }

    // A "belongsTo" is two members: the foreign key column and the navigation property.
    // "= null!" tells the compiler "EF will populate this, stop warning me that it could be null".
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public int UnitTypeId { get; set; }
    public UnitType UnitType { get; set; } = null!;

    public List<Lease> Leases { get; set; } = [];
    public List<RentalApplication> Applications { get; set; } = [];
}
