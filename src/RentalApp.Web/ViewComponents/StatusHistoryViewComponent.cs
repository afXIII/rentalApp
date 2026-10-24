using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Domain;
using RentalApp.Web.Models.Applications;

namespace RentalApp.Web.ViewComponents;

// Status changes and review outcomes (who, when, comment). Property managers only:
// the component renders nothing for anyone else, wherever it is placed.
public class StatusHistoryViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        if (!UserClaimsPrincipal.IsInRole(Roles.PropertyManager))
        {
            return Content("");
        }

        var rows = await db.ApplicationStatusHistory
            .AsNoTracking()
            .Where(h => h.RentalApplicationId == applicationId)
            .OrderByDescending(h => h.ChangedAt).ThenByDescending(h => h.Id)
            .Select(h => new StatusHistoryRowViewModel
            {
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                ChangedByName = h.ChangedBy.FullName,
                ChangedAt = h.ChangedAt,
                Comment = h.Comment,
            })
            .ToListAsync();

        return View(rows);
    }
}
