using DigitalServices.Application.Payments;
using MercadoPago.Error;
using MercadoPago.Webhook;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalServices.Infrastructure.Payments;

public sealed class MercadoPagoWebhookSignatureValidator : IPaymentWebhookSignatureValidator
{
    private readonly MercadoPagoOptions _options;
    private readonly ILogger<MercadoPagoWebhookSignatureValidator> _logger;

    public MercadoPagoWebhookSignatureValidator(
        IOptions<MercadoPagoOptions> options,
        ILogger<MercadoPagoWebhookSignatureValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    public bool IsValid(WebhookSignatureValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(_options.WebhookSecret) ||
            _options.WebhookSignatureToleranceSeconds <= 0)
        {
            _logger.LogError(
                "Mercado Pago webhook signature validation is not configured correctly.");
            return false;
        }

        try
        {
            WebhookSignatureValidator.Validate(
                request.SignatureHeader,
                request.RequestId,
                request.DataId,
                _options.WebhookSecret,
                tolerance: TimeSpan.FromSeconds(_options.WebhookSignatureToleranceSeconds));

            return true;
        }
        catch (InvalidWebhookSignatureException exception)
        {
            _logger.LogWarning(
                "Rejected Mercado Pago webhook signature for request {RequestId}. Reason: {FailureReason}.",
                request.RequestId,
                exception.Reason);

            return false;
        }
        catch (ArgumentNullException)
        {
            _logger.LogError(
                "Mercado Pago webhook signature validation failed because its configuration is invalid.");
            return false;
        }
    }
}
