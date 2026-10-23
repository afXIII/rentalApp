using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;
using RentalApp.Web.Models.Applications;
using RentalApp.Web.Services;

namespace RentalApp.Web.Controllers;

// Applicants browse units with no lease covering today and start an application for one.
[Authorize(Roles = Roles.Applicant)]
public class AvailableUnitsController(AppDbContext db, TimeProvider clock) : AppController
{
    public async Task<IActionResult> Index(int? propertyId, int? minBedrooms, decimal? maxRent)
    {
        var userId = CurrentUserId;

        // Filters are composed onto the query and run in SQL.
        var query = db.Units.AsNoTracking().Where(Unit.IsAvailableOn(clock.Today()));
        if (propertyId is not null)
        {
            query = query.Where(u => u.PropertyId == propertyId);
        }

        if (minBedrooms is not null)
        {
            query = query.Where(u => u.Bedrooms >= minBedrooms);
        }

        if (maxRent is not null)
        {
            query = query.Where(u => u.MonthlyRent <= maxRent);
        }

        var rows = await query
            .OrderBy(u => u.Property.Name).ThenBy(u => u.UnitNumber)
            .Select(u => new AvailableUnitRowViewModel
            {
                UnitId = u.Id,
                UnitNumber = u.UnitNumber,
                PropertyName = u.Property.Name,
                Address = u.Property.AddressLine + ", " + u.Property.City + ", " + u.Property.State,
                UnitTypeName = u.UnitType.Name,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                OpenApplicationId = u.Applications
                    .Where(a => a.ApplicantId == userId && !ApplicationStatusExtensions.Terminal.Contains(a.Status))
                    .Select(a => (int?)a.Id)
                    .FirstOrDefault(),
            })
            .ToListAsync();

        var propertyOptions = await db.Properties
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
            .ToListAsync();

        return View(new AvailableUnitsViewModel
        {
            PropertyId = propertyId,
            MinBedrooms = minBedrooms,
            MaxRent = maxRent,
            PropertyOptions = propertyOptions,
            Rows = rows,
        });
    }
}
