namespace DigitalServices.Application.Checkout;

public sealed record CreateCheckoutResult(
    Guid OrderId,
    string OrderNumber,
    string ExternalReference,
    string PreferenceId,
    Uri CheckoutUrl);
