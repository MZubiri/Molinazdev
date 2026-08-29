namespace DigitalServices.Application.Checkout;

public interface ICheckoutService
{
    Task<CreateCheckoutResult> CreateAsync(
        CreateCheckoutCommand command,
        CancellationToken cancellationToken);
}
