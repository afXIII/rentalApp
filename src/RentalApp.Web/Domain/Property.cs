namespace RentalApp.Web.Domain;

public class Property
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string AddressLine { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string PostalCode { get; set; }

    // Navigation property: the "hasMany" side. EF fills it when you ask with .Include().
    public List<Unit> Units { get; set; } = [];
}
