using System.Diagnostics;
using DigitalServices.Application.Catalog;
using DigitalServices.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace DigitalServices.Web.Controllers;

public sealed class HomeController(ICatalogService catalogService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var services = await catalogService.GetActiveServicesAsync(cancellationToken);
        return View(services);
    }

    [HttpGet("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
