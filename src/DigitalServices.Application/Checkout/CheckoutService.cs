using DigitalServices.Application.Abstractions.Persistence;
using DigitalServices.Application.Common;
using DigitalServices.Application.Payments;
using DigitalServices.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DigitalServices.Application.Checkout;

public sealed class CheckoutService : ICheckoutService
{
    private const decimal IgvRate = 0.18m;

    private readonly ICatalogRepository _catalogRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<CheckoutService> _logger;
    private readonly TimeProvider _timeProvider;

    public CheckoutService(
        ICatalogRepository catalogRepository,
        IClientRepository clientRepository,
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        IPaymentGateway paymentGateway,
        ILogger<CheckoutService> logger,
        TimeProvider? timeProvider = null)
    {
        _catalogRepository = catalogRepository;
        _clientRepository = clientRepository;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<CreateCheckoutResult> CreateAsync(
        CreateCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(command);
        if (command.PackageId == Guid.Empty)
        {
            throw ValidationError(nameof(command.PackageId), "A package identifier is required.");
        }

        var package = await _catalogRepository.GetActivePackageByIdAsync(
            command.PackageId,
            cancellationToken);

        if (package is null)
        {
            throw new ResourceNotFoundException("ServicePackage", command.PackageId);
        }

        string normalizedEmail;
        try
        {
            normalizedEmail = Client.NormalizeEmail(command.Email);
        }
        catch (ArgumentException exception)
        {
            throw ValidationError(nameof(command.Email), exception.Message);
        }

        var now = _timeProvider.GetUtcNow();
        var client = await _clientRepository.GetByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (client is null)
        {
            client = Client.Create(
                command.FullName,
                normalizedEmail,
                command.PhoneNumber,
                command.CompanyName,
                now);

            await _clientRepository.AddAsync(client, cancellationToken);
        }
        else
        {
            client.UpdateContactDetails(
                command.FullName,
                normalizedEmail,
                command.PhoneNumber,
                command.CompanyName,
                now);
        }

        // The persisted package price is the taxable base; checkout adds IGV server-side.
        // The command deliberately has no client-controlled price or tax fields.
        var totalAmount = decimal.Round(
            package.Price * (1m + IgvRate),
            2,
            MidpointRounding.AwayFromZero);

        var order = Order.Create(
            client.Id,
            package.Id,
            totalAmount,
            package.Currency,
            now);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        CreatePaymentPreferenceResult preference;
        try
        {
            preference = await _paymentGateway.CreatePreferenceAsync(
                new CreatePaymentPreferenceRequest(
                    order.Id,
                    order.OrderNumber,
                    order.ExternalReference,
                    package.Id,
                    package.Name,
                    order.TotalAmount,
                    order.Currency,
                    client.FullName,
                    client.Email,
                    client.PhoneNumber,
                    client.CompanyName),
                cancellationToken);
        }
        catch (PaymentGatewayException)
        {
            _logger.LogWarning(
                "Payment preference creation failed for order {OrderId} ({OrderNumber}).",
                order.Id,
                order.OrderNumber);
            throw;
        }

        EnsureValidPreference(preference);

        var payment = Payment.CreateForPreference(
            order.Id,
            _paymentGateway.GatewayName,
            preference.PreferenceId,
            order.TotalAmount,
            order.Currency,
            _timeProvider.GetUtcNow());

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Checkout preference {PreferenceId} created for order {OrderId} ({OrderNumber}).",
            preference.PreferenceId,
            order.Id,
            order.OrderNumber);

        return new CreateCheckoutResult(
            order.Id,
            order.OrderNumber,
            order.ExternalReference,
            preference.PreferenceId,
            preference.CheckoutUrl);
    }

    private static void EnsureValidPreference(CreatePaymentPreferenceResult preference)
    {
        if (preference is null || string.IsNullOrWhiteSpace(preference.PreferenceId))
        {
            throw new PaymentGatewayException("The payment provider returned an invalid preference identifier.");
        }

        if (!preference.CheckoutUrl.IsAbsoluteUri ||
            (preference.CheckoutUrl.Scheme != Uri.UriSchemeHttps &&
             preference.CheckoutUrl.Scheme != Uri.UriSchemeHttp))
        {
            throw new PaymentGatewayException("The payment provider returned an invalid checkout URL.");
        }
    }

    private static ApplicationValidationException ValidationError(string memberName, string message)
    {
        return new ApplicationValidationException(
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [memberName] = [message]
            });
    }
}
