using DigitalServices.Application.Catalog;
using DigitalServices.Application.Checkout;
using DigitalServices.Application.Common;
using DigitalServices.Application.Payments;
using DigitalServices.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DigitalServices.Web.Controllers;

[Route("checkout")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class CheckoutController(
    ICatalogService catalogService,
    ICheckoutService checkoutService,
    ILogger<CheckoutController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(
        Guid packageId,
        CancellationToken cancellationToken)
    {
        if (packageId == Guid.Empty ||
            !await IsActivePackageAsync(packageId, cancellationToken))
        {
            return PackageNotFound();
        }

        return View(new CheckoutViewModel { PackageId = packageId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("PackageId,FullName,Email,PhoneNumber,CompanyName")] CheckoutViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.PackageId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.PackageId), "Selecciona un paquete válido.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var checkout = await checkoutService.CreateAsync(
                new CreateCheckoutCommand
                {
                    PackageId = model.PackageId,
                    FullName = model.FullName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    CompanyName = model.CompanyName
                },
                cancellationToken);

            return Redirect(checkout.CheckoutUrl.AbsoluteUri);
        }
        catch (ApplicationValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
        catch (ResourceNotFoundException)
        {
            return PackageNotFound();
        }
        catch (PaymentGatewayException)
        {
            logger.LogWarning(
                "The payment provider could not start checkout for package {PackageId}.",
                model.PackageId);

            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ModelState.AddModelError(
                string.Empty,
                "Mercado Pago no está disponible en este momento. Inténtalo nuevamente en unos minutos.");
            return View(model);
        }
    }

    [HttpGet("success")]
    public IActionResult Success() => View();

    [HttpGet("failure")]
    public IActionResult Failure() => View();

    [HttpGet("pending")]
    public IActionResult Pending() => View();

    private async Task<bool> IsActivePackageAsync(
        Guid packageId,
        CancellationToken cancellationToken)
    {
        var services = await catalogService.GetActiveServicesAsync(cancellationToken);
        return services.Any(service => service.Packages.Any(package => package.Id == packageId));
    }

    private void AddValidationErrors(ApplicationValidationException exception)
    {
        foreach (var (memberName, messages) in exception.Errors)
        {
            var modelKey = string.IsNullOrWhiteSpace(memberName) ? string.Empty : memberName;
            foreach (var message in messages)
            {
                ModelState.AddModelError(modelKey, message);
            }
        }
    }

    private IActionResult PackageNotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("~/Views/Shared/NotFound.cshtml");
    }
}
