namespace RentalApp.Web.Domain;

// Lookup table. An inactive type stays on units that already use it
// but cannot be chosen for any other unit.
public class UnitType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}
