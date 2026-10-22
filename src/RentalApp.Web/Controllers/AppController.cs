using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Web.Domain;

namespace RentalApp.Web.Controllers;

// Shared helpers for the app's controllers, mainly the modal protocol used by wwwroot/js/site.js.
public abstract class AppController : Controller
{
    // Identity stores the user id in the NameIdentifier claim of the sign-in cookie.
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("No signed-in user.");

    protected bool IsManager => User.IsInRole(Roles.PropertyManager);

    // Validation failed: send the same partial back with 422 so the modal re-renders in place.
    protected PartialViewResult InvalidModal(string viewName, object model)
    {
        Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return PartialView(viewName, model);
    }

    // Saved: 204 tells the modal script to close and refresh the affected part of the page.
    protected NoContentResult ModalSuccess() => NoContent();
}
