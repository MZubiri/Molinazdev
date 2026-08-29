namespace DigitalServices.Application.Payments;

public interface IPaymentGateway
{
    string GatewayName { get; }

    Task<CreatePaymentPreferenceResult> CreatePreferenceAsync(
        CreatePaymentPreferenceRequest request,
        CancellationToken cancellationToken);

    Task<GatewayPaymentDetails?> GetPaymentAsync(
        string gatewayPaymentId,
        CancellationToken cancellationToken);
}
