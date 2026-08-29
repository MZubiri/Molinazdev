using DigitalServices.Domain.Common;

namespace DigitalServices.Domain.Entities;

public sealed class Payment
{
    private Payment()
    {
    }

    private Payment(
        Guid id,
        Guid orderId,
        string gateway,
        string? gatewayPaymentId,
        string? preferenceId,
        decimal amount,
        string currency,
        string status,
        string? statusDetail,
        string? notificationId,
        string? rawPayload,
        DateTimeOffset createdAt,
        DateTimeOffset? processedAt)
    {
        Id = DomainGuard.RequiredId(id, nameof(id));
        OrderId = DomainGuard.RequiredId(orderId, nameof(orderId));
        Gateway = DomainGuard.Required(gateway, DomainFieldLengths.PaymentGateway, nameof(gateway));
        GatewayPaymentId = DomainGuard.Optional(
            gatewayPaymentId,
            DomainFieldLengths.GatewayIdentifier,
            nameof(gatewayPaymentId));
        PreferenceId = DomainGuard.Optional(
            preferenceId,
            DomainFieldLengths.GatewayIdentifier,
            nameof(preferenceId));
        Amount = DomainGuard.PositiveAmount(amount, nameof(amount));
        Currency = DomainGuard.NormalizeCurrency(currency, nameof(currency));
        Status = DomainGuard.Required(status, DomainFieldLengths.PaymentStatus, nameof(status));
        StatusDetail = DomainGuard.Optional(
            statusDetail,
            DomainFieldLengths.PaymentStatusDetail,
            nameof(statusDetail));
        NotificationId = DomainGuard.Optional(
            notificationId,
            DomainFieldLengths.GatewayIdentifier,
            nameof(notificationId));
        RawPayload = string.IsNullOrWhiteSpace(rawPayload) ? null : rawPayload;
        CreatedAt = DomainGuard.UtcTimestamp(createdAt, nameof(createdAt));
        UpdatedAt = CreatedAt;
        ProcessedAt = processedAt is null
            ? null
            : DomainGuard.UtcTimestamp(processedAt.Value, nameof(processedAt));
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string Gateway { get; private set; } = string.Empty;

    public string? GatewayPaymentId { get; private set; }

    public string? PreferenceId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public string? StatusDetail { get; private set; }

    public string? NotificationId { get; private set; }

    public string? RawPayload { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public Order Order { get; private set; } = null!;

    public static Payment CreateForPreference(
        Guid orderId,
        string gateway,
        string preferenceId,
        decimal amount,
        string currency,
        DateTimeOffset createdAt)
    {
        return new Payment(
            Guid.NewGuid(),
            orderId,
            gateway,
            gatewayPaymentId: null,
            preferenceId,
            amount,
            currency,
            status: "preference_created",
            statusDetail: null,
            notificationId: null,
            rawPayload: null,
            createdAt,
            processedAt: null);
    }

    public static Payment CreateVerified(
        Guid orderId,
        string gateway,
        string gatewayPaymentId,
        string? preferenceId,
        decimal amount,
        string currency,
        string status,
        string? statusDetail,
        string? notificationId,
        string? rawPayload,
        DateTimeOffset processedAt)
    {
        return new Payment(
            Guid.NewGuid(),
            orderId,
            gateway,
            gatewayPaymentId,
            preferenceId,
            amount,
            currency,
            status,
            statusDetail,
            notificationId,
            rawPayload,
            processedAt,
            processedAt);
    }

    public void ApplyVerifiedState(
        string gatewayPaymentId,
        string? preferenceId,
        decimal amount,
        string currency,
        string status,
        string? statusDetail,
        string? notificationId,
        string? rawPayload,
        DateTimeOffset processedAt)
    {
        var normalizedPaymentId = DomainGuard.Required(
            gatewayPaymentId,
            DomainFieldLengths.GatewayIdentifier,
            nameof(gatewayPaymentId));

        if (GatewayPaymentId is not null &&
            !string.Equals(GatewayPaymentId, normalizedPaymentId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A payment cannot be reassigned to another gateway payment identifier.");
        }

        var normalizedAmount = DomainGuard.PositiveAmount(amount, nameof(amount));
        var normalizedCurrency = DomainGuard.NormalizeCurrency(currency, nameof(currency));
        if (Amount != normalizedAmount || !string.Equals(Currency, normalizedCurrency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Verified payment amount and currency must match the payment snapshot.");
        }

        GatewayPaymentId = normalizedPaymentId;
        PreferenceId = DomainGuard.Optional(
            preferenceId,
            DomainFieldLengths.GatewayIdentifier,
            nameof(preferenceId)) ?? PreferenceId;
        Status = DomainGuard.Required(status, DomainFieldLengths.PaymentStatus, nameof(status));
        StatusDetail = DomainGuard.Optional(
            statusDetail,
            DomainFieldLengths.PaymentStatusDetail,
            nameof(statusDetail));
        NotificationId = DomainGuard.Optional(
            notificationId,
            DomainFieldLengths.GatewayIdentifier,
            nameof(notificationId));
        RawPayload = string.IsNullOrWhiteSpace(rawPayload) ? RawPayload : rawPayload;
        ProcessedAt = DomainGuard.UtcTimestamp(processedAt, nameof(processedAt));
        UpdatedAt = ProcessedAt.Value;
    }
}
