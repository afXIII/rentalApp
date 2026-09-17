namespace RentalApp.Web.Domain;

public class Residence
{
    public int Id { get; set; }
    public required string Address { get; set; }
    public required string LandlordName { get; set; }
    public required string LandlordPhone { get; set; }

    // DateOnly is a date with no time component, like a Carbon date cast to 'date'.
    public DateOnly MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication RentalApplication { get; set; } = null!;
}
