using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Data;
using RentalApp.Web.Models.Applications;

namespace RentalApp.Web.ViewComponents;

// The Residence History section. A view component (not a partial) because it loads its own
// data, so it can be rendered inside the application page and also returned on its own by
// ResidencesController.List to refresh the list after the residence modal saves.
// Laravel: a Blade class component with its own render() query.
public class ResidenceHistoryViewComponent(AppDbContext db) : ViewComponent
{
    // `editable` is decided by the caller on the server (owner + Draft/Returned + current section).
    public async Task<IViewComponentResult> InvokeAsync(int applicationId, bool editable)
    {
        var rows = await db.Residences
            .AsNoTracking()
            .Where(r => r.RentalApplicationId == applicationId)
            .OrderByDescending(r => r.MoveInDate)
            .Select(r => new ResidenceRowViewModel
            {
                Id = r.Id,
                Address = r.Address,
                LandlordName = r.LandlordName,
                LandlordPhone = r.LandlordPhone,
                MoveInDate = r.MoveInDate,
                MoveOutDate = r.MoveOutDate,
            })
            .ToListAsync();

        return View(new ResidenceHistoryViewModel { ApplicationId = applicationId, IsEditable = editable, Rows = rows });
    }
}
