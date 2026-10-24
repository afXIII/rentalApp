using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;
using RentalApp.Web.Models.Applications;
using RentalApp.Web.Services;

namespace RentalApp.Web.Controllers;

// Property manager review: claim from the queue, release, complete with an outcome, internal notes.
[Authorize(Roles = Roles.PropertyManager)]
public class ReviewController(AppDbContext db, ApplicationWorkflow workflow) : AppController
{
    private const string ReviewPartial = "_ReviewForm";
    private const string NotesPartial = "_NotesForm";

    [HttpPost]
    public Task<IActionResult> Claim(int id) =>
        RunAndRedirectAsync(id, application => workflow.ClaimAsync(application, CurrentUserId),
            "You claimed this application. Complete the review when ready.");

    [HttpPost]
    public Task<IActionResult> Release(int id) =>
        RunAndRedirectAsync(id, application => workflow.ReleaseAsync(application, CurrentUserId),
            "The application is back in the review queue.");

    [HttpGet]
    public async Task<IActionResult> Complete(int id)
    {
        var application = await workflow.FindForUpdateAsync(id);
        if (application is null)
        {
            return NotFound();
        }

        return application.IsClaimedBy(CurrentUserId)
            ? PartialView(ReviewPartial, new ReviewFormViewModel { ApplicationId = id })
            : Forbid();
    }

    [HttpPost]
    public async Task<IActionResult> Complete(ReviewFormViewModel model)
    {
        var application = await workflow.FindForUpdateAsync(model.ApplicationId);
        if (application is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return InvalidModal(ReviewPartial, model);
        }

        try
        {
            await workflow.CompleteReviewAsync(application, CurrentUserId, model.Outcome!.Value, model.Comment);
        }
        catch (DomainRuleException ex)
        {
            // For example: the unit was leased since this application was submitted.
            ModelState.AddModelError("", ex.Message);
            return InvalidModal(ReviewPartial, model);
        }

        TempData["Success"] = model.Outcome switch
        {
            ReviewOutcome.Approve => "Application approved. A twelve-month lease was issued.",
            ReviewOutcome.Return => "Application returned to the applicant for correction.",
            _ => "Application denied.",
        };
        return ModalSuccess();
    }

    [HttpGet]
    public async Task<IActionResult> Notes(int id)
    {
        var notes = await db.RentalApplications.Where(a => a.Id == id).Select(a => new { a.ManagerNotes }).SingleOrDefaultAsync();
        return notes is null
            ? NotFound()
            : PartialView(NotesPartial, new ManagerNotesFormViewModel { ApplicationId = id, Notes = notes.ManagerNotes });
    }

    [HttpPost]
    public async Task<IActionResult> Notes(ManagerNotesFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return InvalidModal(NotesPartial, model);
        }

        // A targeted UPDATE of one column: it does not touch the application's concurrency
        // Version, so editing notes never conflicts with the applicant or a review.
        var updated = await db.RentalApplications
            .Where(a => a.Id == model.ApplicationId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ManagerNotes, string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim()));
        return updated == 0 ? NotFound() : ModalSuccess();
    }

    // Re-renders the notes panel after the notes modal saves.
    public IActionResult NotesPanel(int id) => ViewComponent("ManagerNotes", new { applicationId = id });

    private async Task<IActionResult> RunAndRedirectAsync(int id, Func<RentalApplication, Task> action, string success)
    {
        var application = await workflow.FindForUpdateAsync(id);
        if (application is null)
        {
            return NotFound();
        }

        try
        {
            await action(application);
            TempData["Success"] = success;
        }
        catch (DomainRuleException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Details", "Applications", new { id });
    }
}
