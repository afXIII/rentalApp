using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RentalApp.Web.Models.Properties;

public class PropertyRowViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public int UnitCount { get; init; }
    public int AvailableUnitCount { get; init; }
}

public class PropertyFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [Required, StringLength(200)]
    [Display(Name = "Street address")]
    public string AddressLine { get; set; } = "";

    [Required, StringLength(100)]
    public string City { get; set; } = "";

    [Required, StringLength(50)]
    public string State { get; set; } = "";

    [Required, StringLength(20)]
    [Display(Name = "Postal code")]
    public string PostalCode { get; set; } = "";
}

public class PropertyDetailsViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required UnitTableViewModel Units { get; init; }
}

public class UnitTableViewModel
{
    public int PropertyId { get; init; }
    public required IReadOnlyList<UnitRowViewModel> Rows { get; init; }
}

public class UnitRowViewModel
{
    public int Id { get; init; }
    public required string UnitNumber { get; init; }
    public int Bedrooms { get; init; }
    public decimal MonthlyRent { get; init; }
    public required string UnitTypeName { get; init; }
    public bool UnitTypeIsActive { get; init; }

    // End of the lease covering today, or null when the unit is available.
    public DateOnly? LeasedUntil { get; init; }
}

public class UnitFormViewModel
{
    public int? Id { get; set; }
    public int PropertyId { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Unit number")]
    public string UnitNumber { get; set; } = "";

    [Range(0, 10)]
    public int Bedrooms { get; set; } = 1;

    [Range(typeof(decimal), "1", "100000")]
    [DataType(DataType.Currency)]
    [Display(Name = "Monthly rent")]
    public decimal MonthlyRent { get; set; }

    [Required(ErrorMessage = "Choose a unit type.")]
    [Display(Name = "Unit type")]
    public int? UnitTypeId { get; set; }

    // Filled by the controller for the <select>. Never bound from the request.
    [ValidateNever]
    public IReadOnlyList<SelectListItem> UnitTypeOptions { get; set; } = [];
}
