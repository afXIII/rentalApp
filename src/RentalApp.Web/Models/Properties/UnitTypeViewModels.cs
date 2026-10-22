using System.ComponentModel.DataAnnotations;

namespace RentalApp.Web.Models.Properties;

public class UnitTypeRowViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public bool IsActive { get; init; }
    public int UnitCount { get; init; }
}

public class UnitTypeFormViewModel
{
    [Required, StringLength(50)]
    public string Name { get; set; } = "";
}
