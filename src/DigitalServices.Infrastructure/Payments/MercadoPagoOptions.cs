using System.ComponentModel.DataAnnotations;

namespace DigitalServices.Infrastructure.Payments;

public sealed class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    [Required(AllowEmptyStrings = false)]
    public string AccessToken { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string WebhookSecret { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string SuccessUrl { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string FailureUrl { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string PendingUrl { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string NotificationUrl { get; init; } = string.Empty;

    public bool UseSandbox { get; init; }

    [Range(1, 3_600)]
    public int WebhookSignatureToleranceSeconds { get; init; } = 300;
}
