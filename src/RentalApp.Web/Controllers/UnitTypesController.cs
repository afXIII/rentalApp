using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;
using RentalApp.Web.Models.Properties;

namespace RentalApp.Web.Controllers;

// The Unit Type lookup. Values are deactivated rather than deleted, because units keep using them.
[Authorize(Roles = Roles.PropertyManager)]
public class UnitTypesController(AppDbContext db) : AppController
{
    private const string FormPartial = "_UnitTypeForm";

    public async Task<IActionResult> Index() => View(await LoadRowsAsync());

    public async Task<IActionResult> Table() => PartialView("_UnitTypeTable", await LoadRowsAsync());

    [HttpGet]
    public IActionResult Create() => PartialView(FormPartial, new UnitTypeFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(UnitTypeFormViewModel model)
    {
        var name = model.Name?.Trim();
        if (await db.UnitTypes.AnyAsync(t => t.Name == name))
        {
            ModelState.AddModelError(nameof(model.Name), "A unit type with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return InvalidModal(FormPartial, model);
        }

        db.UnitTypes.Add(new UnitType { Name = name! });
        await db.SaveChangesAsync();
        return ModalSuccess();
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var type = await db.UnitTypes.FindAsync(id);
        if (type is null)
        {
            return NotFound();
        }

        type.IsActive = !type.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = $"{type.Name} is now {(type.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    private Task<List<UnitTypeRowViewModel>> LoadRowsAsync() =>
        db.UnitTypes
            .OrderBy(t => t.Name)
            .Select(t => new UnitTypeRowViewModel
            {
                Id = t.Id,
                Name = t.Name,
                IsActive = t.IsActive,
                UnitCount = db.Units.Count(u => u.UnitTypeId == t.Id),
            })
            .ToListAsync();
}
