using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;
using RentalApp.Web.Models.Applications;

namespace RentalApp.Web.ViewComponents;

// Internal notes on an application. The only place ManagerNotes is read for display, and it
// refuses to render for anyone who is not a property manager.
public class ManagerNotesViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        if (!UserClaimsPrincipal.IsInRole(Roles.PropertyManager))
        {
            return Content("");
        }

        var notes = await db.RentalApplications
            .Where(a => a.Id == applicationId)
            .Select(a => a.ManagerNotes)
            .SingleOrDefaultAsync();

        return View(new ManagerNotesViewModel { ApplicationId = applicationId, Notes = notes });
    }
}
