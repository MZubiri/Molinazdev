namespace DigitalServices.Application.Payments;

public sealed class PaymentGatewayException : Exception
{
    public PaymentGatewayException(string message)
        : base(message)
    {
    }

    public PaymentGatewayException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
