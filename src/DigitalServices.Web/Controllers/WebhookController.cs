using System.Text;
using System.Text.Json;
using DigitalServices.Application.Payments;
using Microsoft.AspNetCore.Mvc;

namespace DigitalServices.Web.Controllers;

[ApiController]
[Route("api/webhooks/mercadopago")]
[IgnoreAntiforgeryToken]
public sealed class WebhookController(
    IPaymentWebhookProcessor webhookProcessor,
    ILogger<WebhookController> logger) : ControllerBase
{
    private const int MaximumPayloadBytes = 256 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaximumPayloadBytes)]
    public async Task<IActionResult> MercadoPago(CancellationToken cancellationToken)
    {
        var signature = Request.Headers["x-signature"].ToString();
        var requestId = Request.Headers["x-request-id"].ToString();
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(requestId))
        {
            logger.LogWarning("Rejected a Mercado Pago webhook without required signature headers.");
            return Unauthorized(new { accepted = false });
        }

        string rawPayload;
        using (var reader = new StreamReader(
                   Request.Body,
                   Encoding.UTF8,
                   detectEncodingFromByteOrderMarks: false,
                   leaveOpen: true))
        {
            rawPayload = await reader.ReadToEndAsync(cancellationToken);
        }

        if (!TryReadEnvelope(rawPayload, Request.Query, out var envelope))
        {
            logger.LogWarning(
                "Rejected malformed Mercado Pago webhook request {RequestId}.",
                requestId);
            return BadRequest(new { accepted = false });
        }

        ProcessPaymentWebhookResult result;
        try
        {
            result = await webhookProcessor.ProcessAsync(
                new ProcessPaymentWebhookCommand(
                    envelope.Type,
                    envelope.DataId,
                    envelope.NotificationId,
                    rawPayload,
                    signature,
                    requestId),
                cancellationToken);
        }
        catch (PaymentGatewayException)
        {
            logger.LogWarning(
                "Mercado Pago could not be queried while handling webhook request {RequestId}.",
                requestId);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { accepted = false });
        }

        return result.Outcome switch
        {
            PaymentWebhookOutcome.Processed or
            PaymentWebhookOutcome.Duplicate or
            PaymentWebhookOutcome.Ignored => Ok(new { accepted = true }),
            PaymentWebhookOutcome.InvalidSignature => Unauthorized(new { accepted = false }),
            PaymentWebhookOutcome.InvalidPayload => BadRequest(new { accepted = false }),
            PaymentWebhookOutcome.PaymentNotFound => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { accepted = false }),
            PaymentWebhookOutcome.OrderNotFound or
            PaymentWebhookOutcome.InconsistentPayment => Conflict(new { accepted = false }),
            _ => StatusCode(
                StatusCodes.Status500InternalServerError,
                new { accepted = false })
        };
    }

    private static bool TryReadEnvelope(
        string rawPayload,
        IQueryCollection query,
        out WebhookEnvelope envelope)
    {
        string? bodyType = null;
        string? bodyDataId = null;
        string? notificationId = null;
        string? action = null;

        if (!string.IsNullOrWhiteSpace(rawPayload))
        {
            try
            {
                using var document = JsonDocument.Parse(rawPayload);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    envelope = default;
                    return false;
                }

                bodyType = GetScalar(document.RootElement, "type");
                action = GetScalar(document.RootElement, "action");
                notificationId = GetScalar(document.RootElement, "id");

                if (document.RootElement.TryGetProperty("data", out var data) &&
                    data.ValueKind == JsonValueKind.Object)
                {
                    bodyDataId = GetScalar(data, "id");
                }
            }
            catch (JsonException)
            {
                envelope = default;
                return false;
            }
        }

        var type = FirstNotBlank(
            query["type"].ToString(),
            query["topic"].ToString(),
            bodyType);

        if (string.IsNullOrWhiteSpace(type) &&
            action?.StartsWith("payment.", StringComparison.OrdinalIgnoreCase) == true)
        {
            type = "payment";
        }

        var queryDataId = FirstNotBlank(query["data.id"].ToString());
        if (queryDataId is not null &&
            bodyDataId is not null &&
            !string.Equals(queryDataId, bodyDataId.Trim(), StringComparison.Ordinal))
        {
            envelope = default;
            return false;
        }

        var dataId = FirstNotBlank(
            queryDataId,
            bodyDataId,
            query["id"].ToString());

        notificationId = FirstNotBlank(
            notificationId,
            query["notification_id"].ToString());

        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(dataId))
        {
            envelope = default;
            return false;
        }

        envelope = new WebhookEnvelope(type, dataId, notificationId);
        return true;
    }

    private static string? GetScalar(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }

    private static string? FirstNotBlank(params string?[] values)
    {
        return values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private readonly record struct WebhookEnvelope(
        string Type,
        string DataId,
        string? NotificationId);
}
