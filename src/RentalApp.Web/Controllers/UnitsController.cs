using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;
using RentalApp.Web.Models.Properties;
using RentalApp.Web.Models.Shared;
using RentalApp.Web.Services;

namespace RentalApp.Web.Controllers;

// Units are always managed in the context of a property (Properties/Details).
[Authorize(Roles = Roles.PropertyManager)]
public class UnitsController(AppDbContext db, TimeProvider clock) : AppController
{
    private const string FormPartial = "_UnitForm";

    // Refreshes the unit table after a modal save.
    public async Task<IActionResult> Table(int propertyId) =>
        PartialView("_UnitTable", await UnitQueries.LoadTableAsync(db, propertyId, clock.Today()));

    [HttpGet]
    public async Task<IActionResult> Create(int propertyId)
    {
        if (!await db.Properties.AnyAsync(p => p.Id == propertyId))
        {
            return NotFound();
        }

        var model = new UnitFormViewModel { PropertyId = propertyId };
        model.UnitTypeOptions = await UnitQueries.UnitTypeOptionsAsync(db, currentUnitTypeId: null);
        return PartialView(FormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(UnitFormViewModel model)
    {
        if (!await db.Properties.AnyAsync(p => p.Id == model.PropertyId))
        {
            return NotFound();
        }

        await ValidateAsync(model, unitId: null, currentUnitTypeId: null);
        if (!ModelState.IsValid)
        {
            model.UnitTypeOptions = await UnitQueries.UnitTypeOptionsAsync(db, currentUnitTypeId: null);
            return InvalidModal(FormPartial, model);
        }

        db.Units.Add(new Unit
        {
            PropertyId = model.PropertyId,
            UnitNumber = model.UnitNumber.Trim(),
            Bedrooms = model.Bedrooms,
            MonthlyRent = model.MonthlyRent,
            UnitTypeId = model.UnitTypeId!.Value,
        });
        await db.SaveChangesAsync();
        return ModalSuccess();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var unit = await db.Units.FindAsync(id);
        if (unit is null)
        {
            return NotFound();
        }

        return PartialView(FormPartial, new UnitFormViewModel
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            UnitNumber = unit.UnitNumber,
            Bedrooms = unit.Bedrooms,
            MonthlyRent = unit.MonthlyRent,
            UnitTypeId = unit.UnitTypeId,
            UnitTypeOptions = await UnitQueries.UnitTypeOptionsAsync(db, unit.UnitTypeId),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UnitFormViewModel model)
    {
        var unit = await db.Units.FindAsync(id);
        if (unit is null)
        {
            return NotFound();
        }

        // The unit's property comes from the database, never from the form.
        model.PropertyId = unit.PropertyId;
        await ValidateAsync(model, unit.Id, unit.UnitTypeId);
        if (!ModelState.IsValid)
        {
            model.UnitTypeOptions = await UnitQueries.UnitTypeOptionsAsync(db, unit.UnitTypeId);
            return InvalidModal(FormPartial, model);
        }

        unit.UnitNumber = model.UnitNumber.Trim();
        unit.Bedrooms = model.Bedrooms;
        unit.MonthlyRent = model.MonthlyRent;
        unit.UnitTypeId = model.UnitTypeId!.Value;
        await db.SaveChangesAsync();
        return ModalSuccess();
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var unit = await db.Units.FindAsync(id);
        return unit is null ? NotFound() : PartialView("_ConfirmModal", DeleteConfirmation(unit));
    }

    [HttpPost, ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var unit = await db.Units.FindAsync(id);
        if (unit is null)
        {
            return NotFound();
        }

        if (await db.Units.AnyAsync(u => u.Id == id && (u.Applications.Any() || u.Leases.Any())))
        {
            ModelState.AddModelError("", "This unit has applications or leases, so it cannot be removed.");
            return InvalidModal("_ConfirmModal", DeleteConfirmation(unit));
        }

        db.Units.Remove(unit);
        await db.SaveChangesAsync();
        return ModalSuccess();
    }

    private ConfirmViewModel DeleteConfirmation(Unit unit) => new()
    {
        Title = "Remove unit",
        Message = $"Remove unit {unit.UnitNumber}?",
        ActionUrl = Url.Action(nameof(Delete), new { id = unit.Id })!,
    };

    // Rules that need the database. Attribute rules on the view model run during model binding.
    private async Task ValidateAsync(UnitFormViewModel model, int? unitId, int? currentUnitTypeId)
    {
        if (model.UnitTypeId is { } typeId)
        {
            // The inactive-type rule is enforced here, not just by hiding options in the dropdown.
            var type = await db.UnitTypes.FindAsync(typeId);
            if (type is null || !type.IsSelectableFor(currentUnitTypeId))
            {
                ModelState.AddModelError(nameof(model.UnitTypeId), "This unit type is inactive and cannot be selected.");
            }
        }

        var number = model.UnitNumber?.Trim();
        var duplicate = await db.Units.AnyAsync(u =>
            u.PropertyId == model.PropertyId && u.UnitNumber == number && u.Id != unitId);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(model.UnitNumber), "This property already has a unit with that number.");
        }
    }
}
