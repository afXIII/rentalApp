using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;
using RentalApp.Web.Models.Applications;
using RentalApp.Web.Models.Shared;
using RentalApp.Web.Services;

namespace RentalApp.Web.Controllers;

public class ApplicationsController(AppDbContext db, ApplicationWorkflow workflow, ApplicationPageBuilder pages) : AppController
{
    // The list page. Rows come from the JSON endpoint (Api/ApplicationsApiController) through the DataGrid view component.
    [HttpGet]
    public async Task<IActionResult> Index(ApplicationStatus? status, int? propertyId)
    {
        var properties = await db.Properties
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
            .ToListAsync();
        var statuses = Enum.GetValues<ApplicationStatus>()
            .Select(s => new SelectListItem(s.DisplayName(), s.ToString()))
            .ToList();

        List<GridColumn> columns =
        [
            new("id", "#", SortKey: "id"),
            new("status", "Status", SortKey: "status", Format: "status"),
            new("propertyName", "Property", SortKey: "property"),
            new("unitNumber", "Unit", SortKey: "unit"),
        ];
        if (IsManager)
        {
            columns.Add(new("applicantName", "Applicant", SortKey: "applicant"));
            columns.Add(new("claimedByName", "Reviewer"));
        }

        columns.Add(new("createdAt", "Started", SortKey: "created", Format: "date"));
        columns.Add(new("submittedAt", "Submitted", SortKey: "submitted", Format: "date"));

        ViewData["Title"] = IsManager ? "Applications" : "My applications";
        return View(new GridDefinition
        {
            Id = "application-grid",
            DataUrl = Url.Action("Get", "ApplicationsApi")!,
            Columns = columns,
            Filters =
            [
                new GridFilter("status", "Status", statuses, status?.ToString()),
                new GridFilter("propertyId", "Property", properties, propertyId?.ToString()),
            ],
            DefaultSort = "created",
            RowLink = Url.Action(nameof(Details)) + "/{id}",
            EmptyText = IsManager ? "No applications match these filters." : "You have no applications yet.",
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Applicant)]
    public async Task<IActionResult> Create(int unitId)
    {
        try
        {
            var application = await workflow.StartAsync(unitId, CurrentUserId);
            return RedirectToAction(nameof(Details), new { id = application.Id });
        }
        catch (DomainRuleException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "AvailableUnits");
        }
    }

    // The application page: one section at a time, chosen by ?step=.
    [HttpGet]
    public async Task<IActionResult> Details(int id, ApplicationStep? step)
    {
        var model = await pages.BuildAsync(id, CurrentUserId, IsManager, step);
        return model is null ? NotFound() : View(model);
    }

    // One form, one action. The clicked button ("command") decides what happens:
    //   continue: validate the current section, save it only if valid, show the next section
    //   back:     show the previous section without saving
    //   submit:   from the Summary, once both sections are saved
    [HttpPost]
    [Authorize(Roles = Roles.Applicant)]
    public async Task<IActionResult> Details(int id, ApplicationStep step, string command,
        [Bind(Prefix = nameof(ApplicationPageViewModel.ApplicantInfo))] ApplicantInfoSection applicantInfo)
    {
        var userId = CurrentUserId;
        var application = await workflow.FindForUpdateAsync(id);
        if (application is null || !application.IsOwnedBy(userId))
        {
            return NotFound();
        }

        if (command == ApplicationCommand.Back)
        {
            return RedirectToStep(id, Previous(step));
        }

        // Reject posts against an application that is no longer editable (submitted, terminal, ...).
        if (!application.CanBeEditedBy(userId))
        {
            TempData["Error"] = $"An application that is {application.Status.DisplayName()} cannot be changed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            switch (command, step)
            {
                case (ApplicationCommand.Continue, ApplicationStep.ApplicantInfo):
                    if (!ModelState.IsValid)
                    {
                        return await RerenderAsync(id, step, applicantInfo);
                    }

                    await workflow.SaveApplicantInfoAsync(application, userId, applicantInfo.FullName!.Trim(),
                        applicantInfo.Phone!.Trim(), applicantInfo.Email!.Trim(), applicantInfo.CurrentAddress!.Trim());
                    return RedirectToStep(id, ApplicationStep.ResidenceHistory);

                case (ApplicationCommand.Continue, ApplicationStep.ResidenceHistory):
                    // Nothing from section 1 is posted on this step, so drop any binding noise for it.
                    ModelState.Clear();
                    await workflow.SaveResidenceHistoryAsync(application, userId);
                    return RedirectToStep(id, ApplicationStep.Summary);

                case (ApplicationCommand.Submit, ApplicationStep.Summary):
                    ModelState.Clear();
                    await workflow.SubmitAsync(application, userId);
                    TempData["Success"] = "Your application was submitted. A property manager will review it.";
                    return RedirectToAction(nameof(Details), new { id });

                default:
                    return BadRequest();
            }
        }
        catch (DomainRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await RerenderAsync(id, step, step == ApplicationStep.ApplicantInfo ? applicantInfo : null);
        }
    }

    [HttpGet]
    [Authorize(Roles = Roles.Applicant)]
    public async Task<IActionResult> Withdraw(int id)
    {
        var application = await workflow.FindForUpdateAsync(id);
        if (application is null || !application.IsOwnedBy(CurrentUserId))
        {
            return NotFound();
        }

        return PartialView("_ConfirmModal", WithdrawConfirmation(id));
    }

    [HttpPost, ActionName(nameof(Withdraw))]
    [Authorize(Roles = Roles.Applicant)]
    public async Task<IActionResult> WithdrawConfirmed(int id)
    {
        var application = await workflow.FindForUpdateAsync(id);
        if (application is null || !application.IsOwnedBy(CurrentUserId))
        {
            return NotFound();
        }

        try
        {
            await workflow.WithdrawAsync(application, CurrentUserId);
            TempData["Success"] = "Your application was withdrawn.";
            return ModalSuccess();
        }
        catch (DomainRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return InvalidModal("_ConfirmModal", WithdrawConfirmation(id));
        }
    }

    private ConfirmViewModel WithdrawConfirmation(int id) => new()
    {
        Title = "Withdraw application",
        Message = "Withdraw this application? This cannot be undone.",
        ActionUrl = Url.Action(nameof(Withdraw), new { id })!,
        ButtonText = "Withdraw",
    };

    // Re-renders the page on the same section, keeping what the user typed and the errors.
    private async Task<IActionResult> RerenderAsync(int id, ApplicationStep step, ApplicantInfoSection? posted)
    {
        var model = await pages.BuildAsync(id, CurrentUserId, IsManager, step);
        if (model is null)
        {
            return NotFound();
        }

        if (posted is not null)
        {
            model.ApplicantInfo = posted;
        }

        return View(nameof(Details), model);
    }

    // Post/Redirect/Get: after a save, redirect so a browser refresh does not re-post the form.
    private RedirectToActionResult RedirectToStep(int id, ApplicationStep step) =>
        RedirectToAction(nameof(Details), new { id, step });

    private static ApplicationStep Previous(ApplicationStep step) =>
        step == ApplicationStep.ApplicantInfo ? step : step - 1;
}
