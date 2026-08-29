using DigitalServices.Domain.Entities;

namespace DigitalServices.Infrastructure.Persistence;

/// <summary>
/// Durable inbox entry used to remember every provider notification, even after a payment receives
/// a newer status notification. It is a persistence concern rather than a domain aggregate.
/// </summary>
internal sealed class PaymentNotificationReceipt
{
    private PaymentNotificationReceipt()
    {
    }

    private PaymentNotificationReceipt(
        Guid id,
        string gateway,
        string notificationId,
        string gatewayPaymentId,
        Guid paymentId,
        DateTimeOffset receivedAt)
    {
        Id = id;
        Gateway = gateway;
        NotificationId = notificationId;
        GatewayPaymentId = gatewayPaymentId;
        PaymentId = paymentId;
        ReceivedAt = receivedAt;
    }

    public Guid Id { get; private set; }

    public string Gateway { get; private set; } = string.Empty;

    public string NotificationId { get; private set; } = string.Empty;

    public string GatewayPaymentId { get; private set; } = string.Empty;

    public Guid PaymentId { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    public Payment Payment { get; private set; } = null!;

    public static PaymentNotificationReceipt Create(
        string gateway,
        string notificationId,
        string gatewayPaymentId,
        Guid paymentId,
        DateTimeOffset receivedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gateway);
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayPaymentId);

        if (paymentId == Guid.Empty)
        {
            throw new ArgumentException("The payment identifier is required.", nameof(paymentId));
        }

        return new PaymentNotificationReceipt(
            Guid.NewGuid(),
            gateway.Trim(),
            notificationId.Trim(),
            gatewayPaymentId.Trim(),
            paymentId,
            receivedAt.ToUniversalTime());
    }
}
