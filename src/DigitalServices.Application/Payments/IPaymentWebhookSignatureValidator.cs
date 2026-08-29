namespace DigitalServices.Application.Payments;

public interface IPaymentWebhookSignatureValidator
{
    bool IsValid(WebhookSignatureValidationRequest request);
}

public sealed record WebhookSignatureValidationRequest(
    string DataId,
    string SignatureHeader,
    string RequestId);
