namespace DigitalServices.Application.Payments;

public interface IPaymentWebhookProcessor
{
    Task<ProcessPaymentWebhookResult> ProcessAsync(
        ProcessPaymentWebhookCommand command,
        CancellationToken cancellationToken);
}

public sealed record ProcessPaymentWebhookCommand(
    string Type,
    string DataId,
    string? NotificationId,
    string RawPayload,
    string SignatureHeader,
    string RequestId);

public enum PaymentWebhookOutcome
{
    Processed = 0,
    Duplicate = 1,
    Ignored = 2,
    InvalidSignature = 3,
    InvalidPayload = 4,
    PaymentNotFound = 5,
    OrderNotFound = 6,
    InconsistentPayment = 7
}

public sealed record ProcessPaymentWebhookResult(
    PaymentWebhookOutcome Outcome,
    Guid? OrderId = null,
    Guid? PaymentId = null,
    string? Detail = null);
