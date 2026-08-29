using DigitalServices.Domain.Enums;

namespace DigitalServices.Application.Payments;

/// <summary>
/// Contains only payment facts already verified against the provider API and the expected order.
/// </summary>
public sealed record VerifiedPaymentPersistenceCommand(
    Guid OrderId,
    string ExpectedExternalReference,
    string Gateway,
    string GatewayPaymentId,
    string? PreferenceId,
    decimal Amount,
    string Currency,
    string Status,
    string? StatusDetail,
    string? NotificationId,
    string RawPayload,
    OrderStatus? TargetOrderStatus,
    DateTimeOffset ProcessedAt);

public enum VerifiedPaymentPersistenceOutcome
{
    Applied = 0,
    DuplicateNotification = 1,
    DuplicateGatewayPayment = 2
}

public sealed record VerifiedPaymentPersistenceResult(
    VerifiedPaymentPersistenceOutcome Outcome,
    Guid PaymentId,
    OrderStatus OrderStatus,
    bool OrderStatusChanged);
