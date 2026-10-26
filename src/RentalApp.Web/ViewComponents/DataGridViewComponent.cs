using Microsoft.AspNetCore.Mvc;
using RentalApp.Web.Models.Shared;

namespace RentalApp.Web.ViewComponents;

// A reusable grid: filters, sortable headers, and a pager around a JSON endpoint.
// Usage: <vc:data-grid definition="gridDefinition"></vc:data-grid>
public class DataGridViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(GridDefinition definition) => View(definition);
}
