namespace DigitalServices.Application.Payments;

public interface IPaymentWebhookStore
{
    /// <summary>
    /// Fast-path duplicate check. Correctness must not depend solely on this pre-check.
    /// </summary>
    Task<bool> HasProcessedNotificationAsync(
        string gateway,
        string notificationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically upserts the payment and transitions its order. Implementations must use one
    /// database transaction and treat unique-key races on gateway payment/notification identifiers
    /// as duplicate outcomes rather than unhandled errors.
    /// </summary>
    Task<VerifiedPaymentPersistenceResult> ApplyVerifiedPaymentAsync(
        VerifiedPaymentPersistenceCommand command,
        CancellationToken cancellationToken);
}
