using DigitalServices.Application.Checkout;
using DigitalServices.Application.Common;
using DigitalServices.Application.Payments;
using Microsoft.AspNetCore.Mvc;

namespace DigitalServices.Web.Controllers.Api;

[ApiController]
[Route("api/checkout")]
public sealed class CheckoutApiController(
    ICheckoutService checkoutService,
    ILogger<CheckoutApiController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateCheckout(
        [FromBody] CreateCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        if (command.PackageId == Guid.Empty)
        {
            return BadRequest(new { message = "Selecciona un paquete válido." });
        }

        try
        {
            var result = await checkoutService.CreateAsync(command, cancellationToken);
            return Ok(new
            {
                checkoutUrl = result.CheckoutUrl.AbsoluteUri,
                preferenceId = result.PreferenceId
            });
        }
        catch (ApplicationValidationException exception)
        {
            return BadRequest(new
            {
                message = "Hay errores en la validación de los datos.",
                errors = exception.Errors
            });
        }
        catch (ResourceNotFoundException)
        {
            return NotFound(new { message = "El paquete seleccionado no existe o ya no está disponible." });
        }
        catch (PaymentGatewayException exception)
        {
            logger.LogWarning(exception, "Mercado Pago no está disponible para el paquete {PackageId}.", command.PackageId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Mercado Pago no está disponible en este momento. Inténtalo nuevamente en unos minutos."
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error inesperado al iniciar checkout para el paquete {PackageId}.", command.PackageId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Ocurrió un error inesperado al procesar la solicitud."
            });
        }
    }
}
