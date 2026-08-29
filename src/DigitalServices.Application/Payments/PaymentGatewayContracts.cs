namespace DigitalServices.Application.Payments;

public sealed record CreatePaymentPreferenceRequest(
    Guid OrderId,
    string OrderNumber,
    string ExternalReference,
    Guid PackageId,
    string ItemTitle,
    decimal Amount,
    string Currency,
    string PayerName,
    string PayerEmail,
    string? PayerPhoneNumber,
    string? PayerCompanyName);

public sealed record CreatePaymentPreferenceResult(
    string PreferenceId,
    Uri CheckoutUrl);

public sealed record GatewayPaymentDetails(
    string GatewayPaymentId,
    string ExternalReference,
    decimal Amount,
    string Currency,
    string Status,
    string? StatusDetail,
    string? PreferenceId);
