using DigitalServices.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace DigitalServices.Web.Controllers;

[Route("servicios")]
public sealed class ServicesController(ICatalogService catalogService) : Controller
{
    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(
        string slug,
        CancellationToken cancellationToken)
    {
        var service = await catalogService.GetActiveServiceBySlugAsync(slug, cancellationToken);
        if (service is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("~/Views/Shared/NotFound.cshtml");
        }

        return View(service);
    }
}
