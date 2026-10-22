using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;
using RentalApp.Web.Models.Properties;
using RentalApp.Web.Models.Shared;
using RentalApp.Web.Services;

namespace RentalApp.Web.Controllers;

[Authorize(Roles = Roles.PropertyManager)]
public class PropertiesController(AppDbContext db, TimeProvider clock) : AppController
{
    private const string FormPartial = "_PropertyForm";

    public async Task<IActionResult> Index() => View(await LoadRowsAsync());

    // Refreshes the table after a modal save.
    public async Task<IActionResult> Table() => PartialView("_PropertyTable", await LoadRowsAsync());

    public async Task<IActionResult> Details(int id)
    {
        var property = await db.Properties.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
        if (property is null)
        {
            return NotFound();
        }

        return View(new PropertyDetailsViewModel
        {
            Id = property.Id,
            Name = property.Name,
            Address = $"{property.AddressLine}, {property.City}, {property.State} {property.PostalCode}",
            Units = await UnitQueries.LoadTableAsync(db, property.Id, clock.Today()),
        });
    }

    [HttpGet]
    public IActionResult Create() => PartialView(FormPartial, new PropertyFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(PropertyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return InvalidModal(FormPartial, model);
        }

        var property = new Property
        {
            Name = model.Name, AddressLine = model.AddressLine, City = model.City, State = model.State, PostalCode = model.PostalCode,
        };
        db.Properties.Add(property);
        await db.SaveChangesAsync();
        return ModalSuccess();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var property = await db.Properties.FindAsync(id);
        if (property is null)
        {
            return NotFound();
        }

        return PartialView(FormPartial, new PropertyFormViewModel
        {
            Id = property.Id,
            Name = property.Name,
            AddressLine = property.AddressLine,
            City = property.City,
            State = property.State,
            PostalCode = property.PostalCode,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, PropertyFormViewModel model)
    {
        var property = await db.Properties.FindAsync(id);
        if (property is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return InvalidModal(FormPartial, model);
        }

        property.Name = model.Name;
        property.AddressLine = model.AddressLine;
        property.City = model.City;
        property.State = model.State;
        property.PostalCode = model.PostalCode;
        await db.SaveChangesAsync();
        return ModalSuccess();
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var property = await db.Properties.FindAsync(id);
        return property is null ? NotFound() : PartialView("_ConfirmModal", DeleteConfirmation(property));
    }

    [HttpPost, ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var property = await db.Properties.FindAsync(id);
        if (property is null)
        {
            return NotFound();
        }

        // Applications and leases are history: a property that has any is kept.
        var inUse = await db.Units.AnyAsync(u => u.PropertyId == id && (u.Applications.Any() || u.Leases.Any()));
        if (inUse)
        {
            ModelState.AddModelError("", "This property has units with applications or leases, so it cannot be removed.");
            return InvalidModal("_ConfirmModal", DeleteConfirmation(property));
        }

        db.Properties.Remove(property); // its units are removed by the cascade rule
        await db.SaveChangesAsync();
        return ModalSuccess();
    }

    private ConfirmViewModel DeleteConfirmation(Property property) => new()
    {
        Title = "Remove property",
        Message = $"Remove {property.Name} and all of its units?",
        ActionUrl = Url.Action(nameof(Delete), new { id = property.Id })!,
    };

    private async Task<List<PropertyRowViewModel>> LoadRowsAsync()
    {
        var today = clock.Today();
        return await db.Properties
            .OrderBy(p => p.Name)
            .Select(p => new PropertyRowViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.AddressLine + ", " + p.City + ", " + p.State,
                UnitCount = p.Units.Count,
                AvailableUnitCount = p.Units.Count(u => !u.Leases.Any(l => l.StartDate <= today && l.EndDate >= today)),
            })
            .ToListAsync();
    }
}
