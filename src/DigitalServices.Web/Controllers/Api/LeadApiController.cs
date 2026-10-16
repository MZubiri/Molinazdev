using DigitalServices.Application.Leads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DigitalServices.Web.Controllers.Api;

[ApiController]
[Route("api/leads")]
[EnableRateLimiting("ChatPolicy")]
public sealed class LeadApiController(
    IAuditLeadNotificationService notificationService,
    ILogger<LeadApiController> logger) : ControllerBase
{
    public sealed record AuditFormRequest(string? WebsiteUrl, string? Contact);

    [HttpPost("audit")]
    public async Task<IActionResult> SubmitAudit(
        [FromBody] AuditFormRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.WebsiteUrl) || string.IsNullOrWhiteSpace(request?.Contact))
        {
            return BadRequest(new { success = false, message = "La dirección web y tu información de contacto son requeridas." });
        }

        var trimmedUrl = request.WebsiteUrl.Trim();
        var trimmedContact = request.Contact.Trim();

        if (trimmedUrl.Length > 300 || trimmedContact.Length > 200)
        {
            return BadRequest(new { success = false, message = "Los campos exceden la longitud máxima permitida." });
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        logger.LogInformation("Processing web audit lead from IP {ClientIp} for {WebsiteUrl}", clientIp, trimmedUrl);

        await notificationService.NotifyAuditLeadAsync(
            new AuditLeadRequestDto(trimmedUrl, trimmedContact),
            clientIp,
            cancellationToken);

        return Ok(new
        {
            success = true,
            message = "¡Solicitud de auditoría recibida con éxito! Nuestro equipo técnico evaluará tu web en menos de 24 horas."
        });
    }
}
