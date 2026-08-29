using DigitalServices.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace DigitalServices.Web.Controllers.Api;

[ApiController]
[Route("api")]
public sealed class CatalogApiController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var services = await catalogService.GetActiveServicesAsync(cancellationToken);
        return Ok(services);
    }

    [HttpGet("services/{slug}")]
    public async Task<IActionResult> GetServiceBySlug(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return BadRequest(new { message = "El slug es obligatorio." });
        }

        var service = await catalogService.GetActiveServiceBySlugAsync(slug, cancellationToken);
        if (service is null)
        {
            return NotFound(new { message = "Servicio no encontrado." });
        }

        return Ok(service);
    }
}
