using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Web.Domain;
using RentalApp.Web.Models;

namespace RentalApp.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    // Signed-in users land on the page they use most; everyone else sees the welcome page.
    public IActionResult Index()
    {
        if (User.IsInRole(Roles.PropertyManager))
        {
            return RedirectToAction("Index", "Applications");
        }

        if (User.IsInRole(Roles.Applicant))
        {
            return RedirectToAction("Index", "AvailableUnits");
        }

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
