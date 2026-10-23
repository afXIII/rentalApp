using Microsoft.AspNetCore.Mvc.Rendering;

namespace RentalApp.Web.Models.Applications;

public class AvailableUnitsViewModel
{
    // Filters, bound from the query string.
    public int? PropertyId { get; init; }
    public int? MinBedrooms { get; init; }
    public decimal? MaxRent { get; init; }

    public required IReadOnlyList<SelectListItem> PropertyOptions { get; init; }
    public required IReadOnlyList<AvailableUnitRowViewModel> Rows { get; init; }
}

public class AvailableUnitRowViewModel
{
    public int UnitId { get; init; }
    public required string UnitNumber { get; init; }
    public required string PropertyName { get; init; }
    public required string Address { get; init; }
    public required string UnitTypeName { get; init; }
    public int Bedrooms { get; init; }
    public decimal MonthlyRent { get; init; }

    // The signed-in applicant's open application for this unit, if any.
    public int? OpenApplicationId { get; init; }
}
