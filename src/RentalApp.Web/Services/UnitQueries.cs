using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Models.Properties;

namespace RentalApp.Web.Services;

// Read-side queries shared by the property and unit controllers.
public static class UnitQueries
{
    public static async Task<UnitTableViewModel> LoadTableAsync(AppDbContext db, int propertyId, DateOnly today)
    {
        var rows = await db.Units
            .Where(u => u.PropertyId == propertyId)
            .OrderBy(u => u.UnitNumber)
            .Select(u => new UnitRowViewModel
            {
                Id = u.Id,
                UnitNumber = u.UnitNumber,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                UnitTypeName = u.UnitType.Name,
                UnitTypeIsActive = u.UnitType.IsActive,
                LeasedUntil = u.Leases
                    .Where(l => l.StartDate <= today && l.EndDate >= today)
                    .Select(l => (DateOnly?)l.EndDate)
                    .FirstOrDefault(),
            })
            .ToListAsync();
        return new UnitTableViewModel { PropertyId = propertyId, Rows = rows };
    }

    // Active types, plus the unit's current type even if it has since been deactivated,
    // so editing a unit never silently changes its type.
    public static async Task<IReadOnlyList<SelectListItem>> UnitTypeOptionsAsync(AppDbContext db, int? currentUnitTypeId)
    {
        return await db.UnitTypes
            .Where(t => t.IsActive || t.Id == currentUnitTypeId)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem(t.IsActive ? t.Name : t.Name + " (inactive)", t.Id.ToString()))
            .ToListAsync();
    }
}
