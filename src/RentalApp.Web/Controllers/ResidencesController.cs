using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;
using RentalApp.Web.Models.Applications;
using RentalApp.Web.Models.Shared;

namespace RentalApp.Web.Controllers;

// Residence History rows, added, edited, and removed through a modal on the application page.
// Every action checks that the signed-in applicant owns the application and that it is editable.
[Authorize(Roles = Roles.Applicant)]
public class ResidencesController(AppDbContext db) : AppController
{
    private const string FormPartial = "_ResidenceForm";

    // Re-renders the residence list after a modal save.
    public async Task<IActionResult> List(int applicationId)
    {
        var application = await db.RentalApplications.AsNoTracking().SingleOrDefaultAsync(a => a.Id == applicationId);
        if (application is null || !application.IsOwnedBy(CurrentUserId))
        {
            return NotFound();
        }

        return ViewComponent("ResidenceHistory", new { applicationId, editable = application.CanBeEditedBy(CurrentUserId) });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int applicationId)
    {
        var check = await CheckEditableAsync(applicationId);
        return check ?? PartialView(FormPartial, new ResidenceFormViewModel { ApplicationId = applicationId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(ResidenceFormViewModel model)
    {
        if (await CheckEditableAsync(model.ApplicationId) is { } rejected)
        {
            return rejected;
        }

        if (!ModelState.IsValid)
        {
            return InvalidModal(FormPartial, model);
        }

        db.Residences.Add(new Residence
        {
            RentalApplicationId = model.ApplicationId,
            Address = model.Address.Trim(),
            LandlordName = model.LandlordName.Trim(),
            LandlordPhone = model.LandlordPhone.Trim(),
            MoveInDate = model.MoveInDate!.Value,
            MoveOutDate = model.MoveOutDate!.Value,
        });
        await db.SaveChangesAsync();
        return ModalSuccess();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var residence = await db.Residences.FindAsync(id);
        if (residence is null)
        {
            return NotFound();
        }

        return await CheckEditableAsync(residence.RentalApplicationId) ?? PartialView(FormPartial, new ResidenceFormViewModel
        {
            Id = residence.Id,
            ApplicationId = residence.RentalApplicationId,
            Address = residence.Address,
            LandlordName = residence.LandlordName,
            LandlordPhone = residence.LandlordPhone,
            MoveInDate = residence.MoveInDate,
            MoveOutDate = residence.MoveOutDate,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ResidenceFormViewModel model)
    {
        var residence = await db.Residences.FindAsync(id);
        if (residence is null)
        {
            return NotFound();
        }

        // The owning application comes from the database, never from the form.
        if (await CheckEditableAsync(residence.RentalApplicationId) is { } rejected)
        {
            return rejected;
        }

        model.ApplicationId = residence.RentalApplicationId;
        if (!ModelState.IsValid)
        {
            return InvalidModal(FormPartial, model);
        }

        residence.Address = model.Address.Trim();
        residence.LandlordName = model.LandlordName.Trim();
        residence.LandlordPhone = model.LandlordPhone.Trim();
        residence.MoveInDate = model.MoveInDate!.Value;
        residence.MoveOutDate = model.MoveOutDate!.Value;
        await db.SaveChangesAsync();
        return ModalSuccess();
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var residence = await db.Residences.FindAsync(id);
        if (residence is null)
        {
            return NotFound();
        }

        return await CheckEditableAsync(residence.RentalApplicationId)
               ?? PartialView("_ConfirmModal", DeleteConfirmation(residence));
    }

    [HttpPost, ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var residence = await db.Residences.FindAsync(id);
        if (residence is null)
        {
            return NotFound();
        }

        if (await CheckEditableAsync(residence.RentalApplicationId) is { } rejected)
        {
            return rejected;
        }

        db.Residences.Remove(residence);
        await db.SaveChangesAsync();
        return ModalSuccess();
    }

    private ConfirmViewModel DeleteConfirmation(Residence residence) => new()
    {
        Title = "Remove residence",
        Message = $"Remove {residence.Address} from your residence history?",
        ActionUrl = Url.Action(nameof(Delete), new { id = residence.Id })!,
        ButtonText = "Remove",
    };

    // Null when the user may change the application's residences; otherwise the result to return.
    // 404 hides other people's applications. 403 rejects changes to an application that is not editable.
    private async Task<IActionResult?> CheckEditableAsync(int applicationId)
    {
        var application = await db.RentalApplications.AsNoTracking().SingleOrDefaultAsync(a => a.Id == applicationId);
        if (application is null || !application.IsOwnedBy(CurrentUserId))
        {
            return NotFound();
        }

        return application.CanBeEditedBy(CurrentUserId) ? null : Forbid();
    }
}
