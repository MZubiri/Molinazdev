using DigitalServices.Application.Abstractions.Persistence;
using DigitalServices.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DigitalServices.Application.Payments;

public sealed class PaymentWebhookProcessor : IPaymentWebhookProcessor
{
    private readonly IPaymentWebhookSignatureValidator _signatureValidator;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentWebhookStore _webhookStore;
    private readonly ILogger<PaymentWebhookProcessor> _logger;
    private readonly TimeProvider _timeProvider;

    public PaymentWebhookProcessor(
        IPaymentWebhookSignatureValidator signatureValidator,
        IPaymentGateway paymentGateway,
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IPaymentWebhookStore webhookStore,
        ILogger<PaymentWebhookProcessor> logger,
        TimeProvider? timeProvider = null)
    {
        _signatureValidator = signatureValidator;
        _paymentGateway = paymentGateway;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _webhookStore = webhookStore;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ProcessPaymentWebhookResult> ProcessAsync(
        ProcessPaymentWebhookCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dataId = command.DataId?.Trim();
        if (string.IsNullOrWhiteSpace(dataId) ||
            string.IsNullOrWhiteSpace(command.SignatureHeader) ||
            string.IsNullOrWhiteSpace(command.RequestId))
        {
            return Result(PaymentWebhookOutcome.InvalidPayload, "Required webhook data is missing.");
        }

        if (!_signatureValidator.IsValid(new WebhookSignatureValidationRequest(
                dataId,
                command.SignatureHeader,
                command.RequestId)))
        {
            _logger.LogWarning("Rejected webhook with an invalid payment-provider signature.");
            return Result(PaymentWebhookOutcome.InvalidSignature, "Webhook signature is invalid.");
        }

        if (!string.Equals(command.Type?.Trim(), "payment", StringComparison.OrdinalIgnoreCase))
        {
            return Result(PaymentWebhookOutcome.Ignored, "Webhook type is not handled.");
        }

        var notificationId = string.IsNullOrWhiteSpace(command.NotificationId)
            ? null
            : command.NotificationId.Trim();

        if (notificationId is not null &&
            await _webhookStore.HasProcessedNotificationAsync(
                _paymentGateway.GatewayName,
                notificationId,
                cancellationToken))
        {
            return Result(PaymentWebhookOutcome.Duplicate, "Notification was already processed.");
        }

        GatewayPaymentDetails? providerPayment;
        try
        {
            providerPayment = await _paymentGateway.GetPaymentAsync(dataId, cancellationToken);
        }
        catch (PaymentGatewayException)
        {
            _logger.LogWarning("Could not verify provider payment {GatewayPaymentId}.", dataId);
            throw;
        }

        if (providerPayment is null)
        {
            _logger.LogWarning("Provider payment {GatewayPaymentId} was not found.", dataId);
            return Result(PaymentWebhookOutcome.PaymentNotFound, "Provider payment was not found.");
        }

        if (!string.Equals(providerPayment.GatewayPaymentId?.Trim(), dataId, StringComparison.Ordinal))
        {
            return Inconsistent("The verified provider payment identifier does not match the notification.");
        }

        if (!Order.TryParseExternalReference(providerPayment.ExternalReference, out var referencedOrderId))
        {
            return Inconsistent("The provider payment has an invalid external reference.");
        }

        var order = await _orderRepository.GetByExternalReferenceAsync(
            providerPayment.ExternalReference,
            cancellationToken);

        if (order is null)
        {
            _logger.LogWarning(
                "No order matches the external reference on provider payment {GatewayPaymentId}.",
                dataId);
            return Result(PaymentWebhookOutcome.OrderNotFound, "The referenced order was not found.");
        }

        if (order.Id != referencedOrderId ||
            !string.Equals(order.ExternalReference, providerPayment.ExternalReference, StringComparison.Ordinal))
        {
            return Inconsistent("The external reference does not resolve to the expected internal order.", order.Id);
        }

        var providerCurrency = providerPayment.Currency?.Trim().ToUpperInvariant();
        if (providerPayment.Amount <= 0m ||
            providerCurrency is null ||
            providerCurrency.Length != 3 ||
            !providerCurrency.All(static character => character is >= 'A' and <= 'Z') ||
            providerPayment.Amount != order.TotalAmount ||
            !string.Equals(providerCurrency, order.Currency, StringComparison.Ordinal))
        {
            _logger.LogError(
                "Payment consistency check failed for order {OrderId} and provider payment {GatewayPaymentId}.",
                order.Id,
                dataId);
            return Inconsistent("Payment amount or currency does not match the order snapshot.", order.Id);
        }

        var preferenceId = string.IsNullOrWhiteSpace(providerPayment.PreferenceId)
            ? null
            : providerPayment.PreferenceId.Trim();

        if (preferenceId is not null)
        {
            var preferencePayment = await _paymentRepository.GetByPreferenceIdAsync(
                _paymentGateway.GatewayName,
                preferenceId,
                cancellationToken);

            if (preferencePayment is not null && preferencePayment.OrderId != order.Id)
            {
                return Inconsistent("The preference belongs to a different internal order.", order.Id);
            }
        }

        if (string.IsNullOrWhiteSpace(providerPayment.Status))
        {
            return Inconsistent("The provider payment status is missing.", order.Id);
        }

        var mapping = MercadoPagoPaymentStatusMapper.Map(providerPayment.Status);
        if (!mapping.IsKnown)
        {
            _logger.LogWarning(
                "Unknown provider payment status {PaymentStatus} for order {OrderId}; order state will not change.",
                providerPayment.Status,
                order.Id);
        }

        var persisted = await _webhookStore.ApplyVerifiedPaymentAsync(
            new VerifiedPaymentPersistenceCommand(
                order.Id,
                order.ExternalReference,
                _paymentGateway.GatewayName,
                dataId,
                preferenceId,
                providerPayment.Amount,
                providerCurrency,
                mapping.NormalizedStatus,
                providerPayment.StatusDetail,
                notificationId,
                command.RawPayload ?? string.Empty,
                mapping.TargetOrderStatus,
                _timeProvider.GetUtcNow()),
            cancellationToken);

        if (persisted.Outcome is VerifiedPaymentPersistenceOutcome.DuplicateGatewayPayment or
            VerifiedPaymentPersistenceOutcome.DuplicateNotification)
        {
            return new ProcessPaymentWebhookResult(
                PaymentWebhookOutcome.Duplicate,
                order.Id,
                persisted.PaymentId,
                "Payment notification was already applied.");
        }

        _logger.LogInformation(
            "Provider payment {GatewayPaymentId} processed for order {OrderId}; order status is {OrderStatus}.",
            dataId,
            order.Id,
            persisted.OrderStatus);

        return new ProcessPaymentWebhookResult(
            PaymentWebhookOutcome.Processed,
            order.Id,
            persisted.PaymentId);
    }

    private static ProcessPaymentWebhookResult Result(PaymentWebhookOutcome outcome, string detail)
    {
        return new ProcessPaymentWebhookResult(outcome, Detail: detail);
    }

    private static ProcessPaymentWebhookResult Inconsistent(string detail, Guid? orderId = null)
    {
        return new ProcessPaymentWebhookResult(
            PaymentWebhookOutcome.InconsistentPayment,
            orderId,
            Detail: detail);
    }
}
