using System.Globalization;
using System.Net.Mail;
using DigitalServices.Application.Payments;
using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Error;
using MercadoPago.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalServices.Infrastructure.Payments;

public sealed class MercadoPagoPaymentGateway : IPaymentGateway
{
    public const string Gateway = "MercadoPago";

    private readonly MercadoPagoOptions _options;
    private readonly ILogger<MercadoPagoPaymentGateway> _logger;
    private readonly PreferenceClient _preferenceClient = new();
    private readonly PaymentClient _paymentClient = new();

    public MercadoPagoPaymentGateway(
        IOptions<MercadoPagoOptions> options,
        ILogger<MercadoPagoPaymentGateway> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    public string GatewayName => Gateway;

    public async Task<CreatePaymentPreferenceResult> CreatePreferenceAsync(
        CreatePaymentPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidatePreferenceRequest(request);

        var (payerName, payerSurname) = SplitPayerName(request.PayerName);
        var preferenceRequest = new PreferenceRequest
        {
            Items =
            [
                new PreferenceItemRequest
                {
                    Id = request.PackageId.ToString("D"),
                    Title = request.ItemTitle,
                    Description = $"Order {request.OrderNumber}",
                    Quantity = 1,
                    UnitPrice = request.Amount,
                    CurrencyId = request.Currency,
                },
            ],
            Payer = new PreferencePayerRequest
            {
                Name = payerName,
                Surname = payerSurname,
                Email = request.PayerEmail,
                Phone = CreatePhone(request.PayerPhoneNumber),
            },
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = _options.SuccessUrl,
                Failure = _options.FailureUrl,
                Pending = _options.PendingUrl,
            },
            AutoReturn = "approved",
            ExternalReference = request.ExternalReference,
            NotificationUrl = _options.NotificationUrl,
            Metadata = new Dictionary<string, object>
            {
                ["internal_order_id"] = request.OrderId.ToString("D"),
                ["order_number"] = request.OrderNumber,
                ["package_id"] = request.PackageId.ToString("D"),
            },
        };

        try
        {
            var requestOptions = CreateRequestOptions(request.OrderId);
            var preference = await _preferenceClient.CreateAsync(
                preferenceRequest,
                requestOptions,
                cancellationToken);

            if (preference is null || string.IsNullOrWhiteSpace(preference.Id))
            {
                throw new PaymentGatewayException(
                    "Mercado Pago returned a preference without an identifier.");
            }

            var checkoutUrlValue = _options.UseSandbox
                ? preference.SandboxInitPoint
                : preference.InitPoint;

            if (!Uri.TryCreate(checkoutUrlValue, UriKind.Absolute, out var checkoutUrl) ||
                !string.Equals(checkoutUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new PaymentGatewayException(
                    "Mercado Pago returned an invalid checkout URL for the configured environment.");
            }

            if (!string.IsNullOrWhiteSpace(preference.ExternalReference) &&
                !string.Equals(
                    preference.ExternalReference,
                    request.ExternalReference,
                    StringComparison.Ordinal))
            {
                throw new PaymentGatewayException(
                    "Mercado Pago returned a preference with an inconsistent external reference.");
            }

            _logger.LogInformation(
                "Created Mercado Pago preference {PreferenceId} for order {OrderNumber}.",
                preference.Id,
                request.OrderNumber);

            return new CreatePaymentPreferenceResult(preference.Id, checkoutUrl);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (MercadoPagoApiException exception)
        {
            _logger.LogError(
                "Mercado Pago returned HTTP {StatusCode} while creating the preference for order {OrderNumber}.",
                exception.StatusCode,
                request.OrderNumber);

            throw new PaymentGatewayException(
                "Mercado Pago could not create the payment preference.",
                exception);
        }
        catch (MercadoPagoException exception)
        {
            _logger.LogError(
                "The Mercado Pago SDK failed with {ExceptionType} while creating the preference for order {OrderNumber}.",
                exception.GetType().Name,
                request.OrderNumber);

            throw new PaymentGatewayException(
                "Mercado Pago is unavailable while creating the payment preference.",
                exception);
        }
    }

    public async Task<GatewayPaymentDetails?> GetPaymentAsync(
        string gatewayPaymentId,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(
                gatewayPaymentId,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var paymentId) ||
            paymentId <= 0)
        {
            throw new PaymentGatewayException("The Mercado Pago payment identifier is invalid.");
        }

        try
        {
            var payment = await _paymentClient.GetAsync(
                paymentId,
                CreateRequestOptions(),
                cancellationToken);

            if (payment is null || payment.Id is null || payment.Id <= 0 || payment.Id.Value != paymentId)
            {
                throw new PaymentGatewayException(
                    "Mercado Pago returned an inconsistent payment identifier.");
            }

            var externalReference = RequireProviderValue(
                payment.ExternalReference,
                "external reference");
            var currency = RequireCurrency(payment.CurrencyId, "payment currency");
            var status = RequireProviderValue(payment.Status, "payment status");

            if (payment.TransactionAmount is null || payment.TransactionAmount <= 0)
            {
                throw new PaymentGatewayException(
                    "Mercado Pago returned an invalid payment amount.");
            }

            _logger.LogDebug(
                "Retrieved Mercado Pago payment {GatewayPaymentId} with status {PaymentStatus}.",
                gatewayPaymentId,
                status);

            return new GatewayPaymentDetails(
                payment.Id.Value.ToString(CultureInfo.InvariantCulture),
                externalReference,
                payment.TransactionAmount.Value,
                currency,
                status,
                payment.StatusDetail,
                PreferenceId: null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (MPNotFoundException)
        {
            _logger.LogWarning(
                "Mercado Pago payment {GatewayPaymentId} was not found.",
                gatewayPaymentId);

            return null;
        }
        catch (MercadoPagoApiException exception)
        {
            _logger.LogError(
                "Mercado Pago returned HTTP {StatusCode} while retrieving payment {GatewayPaymentId}.",
                exception.StatusCode,
                gatewayPaymentId);

            throw new PaymentGatewayException(
                "Mercado Pago could not retrieve the payment.",
                exception);
        }
        catch (MercadoPagoException exception)
        {
            _logger.LogError(
                "The Mercado Pago SDK failed with {ExceptionType} while retrieving payment {GatewayPaymentId}.",
                exception.GetType().Name,
                gatewayPaymentId);

            throw new PaymentGatewayException(
                "Mercado Pago is unavailable while retrieving the payment.",
                exception);
        }
    }

    private RequestOptions CreateRequestOptions(Guid? idempotencyKey = null)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new PaymentGatewayException("Mercado Pago credentials are not configured.");
        }

        var requestOptions = new RequestOptions
        {
            AccessToken = _options.AccessToken,
        };

        if (idempotencyKey.HasValue)
        {
            requestOptions.CustomHeaders.Add(
                Headers.IDEMPOTENCY_KEY,
                idempotencyKey.Value.ToString("D"));
        }

        return requestOptions;
    }

    private static PhoneRequest? CreatePhone(string? phoneNumber)
    {
        return string.IsNullOrWhiteSpace(phoneNumber)
            ? null
            : new PhoneRequest { Number = phoneNumber.Trim() };
    }

    private static (string Name, string Surname) SplitPayerName(string fullName)
    {
        var normalizedName = fullName.Trim();
        var separatorIndex = normalizedName.IndexOf(' ');

        return separatorIndex < 0
            ? (normalizedName, string.Empty)
            : (
                normalizedName[..separatorIndex],
                normalizedName[(separatorIndex + 1)..].Trim());
    }

    private static void ValidatePreferenceRequest(CreatePaymentPreferenceRequest request)
    {
        if (request.OrderId == Guid.Empty)
        {
            throw new PaymentGatewayException("The internal order identifier is required.");
        }

        if (request.PackageId == Guid.Empty)
        {
            throw new PaymentGatewayException("The package identifier is required.");
        }

        RequireRequestValue(request.OrderNumber, "order number");
        RequireRequestValue(request.ExternalReference, "external reference");
        RequireRequestValue(request.ItemTitle, "item title");
        RequireRequestValue(request.PayerName, "payer name");

        if (request.Amount <= 0)
        {
            throw new PaymentGatewayException("The preference amount must be greater than zero.");
        }

        RequireCurrency(request.Currency, "preference currency");

        if (!MailAddress.TryCreate(request.PayerEmail, out _))
        {
            throw new PaymentGatewayException("The payer email address is invalid.");
        }
    }

    private static string RequireRequestValue(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PaymentGatewayException($"The {fieldName} is required.");
        }

        return value;
    }

    private static string RequireProviderValue(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PaymentGatewayException(
                $"Mercado Pago returned a payment without a valid {fieldName}.");
        }

        return value;
    }

    private static string RequireCurrency(string? currency, string fieldName)
    {
        if (currency is null ||
            currency.Length != 3 ||
            currency.Any(static character => character is < 'A' or > 'Z'))
        {
            throw new PaymentGatewayException($"The {fieldName} must be a three-letter ISO code.");
        }

        return currency;
    }
}
