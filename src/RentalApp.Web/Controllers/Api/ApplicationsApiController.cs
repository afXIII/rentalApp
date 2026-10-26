using Microsoft.AspNetCore.Mvc;
using RentalApp.Web.Services;

namespace RentalApp.Web.Controllers.Api;

// JSON endpoint behind the application grid. Uses the same sign-in cookie as the pages.
// Documented at /openapi/v1.json, browsable at /swagger in Development.
[ApiController]
[Route("api/applications")]
[Produces("application/json")]
public class ApplicationsApiController(ApplicationListService list) : AppController
{
    [HttpGet]
    [EndpointSummary("List rental applications")]
    [EndpointDescription(
        "One page of applications, filtered, sorted, and paged in the database, with the total number that matched. " +
        "Applicants receive only their own applications; property managers receive all of them.")]
    [ProducesResponseType<PagedResult<ApplicationListRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<ApplicationListRow>>> Get([FromQuery] ApplicationListRequest request) =>
        Ok(await list.GetPageAsync(request, CurrentUserId, IsManager));
}
