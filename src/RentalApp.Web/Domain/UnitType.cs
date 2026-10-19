namespace RentalApp.Web.Domain;

// Lookup table. An inactive type stays on units that already use it
// but cannot be chosen for any other unit.
public class UnitType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;

    // currentUnitTypeId is the type the unit has today (null when creating a unit).
    // Keeping an inactive type on the unit that already has it is allowed.
    public bool IsSelectableFor(int? currentUnitTypeId) => IsActive || Id == currentUnitTypeId;
}
