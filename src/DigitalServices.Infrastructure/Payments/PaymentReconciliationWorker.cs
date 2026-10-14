using DigitalServices.Application.Abstractions.Persistence;
using DigitalServices.Application.Notifications;
using DigitalServices.Application.Payments;
using DigitalServices.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalServices.Infrastructure.Payments;

public sealed class PaymentReconciliationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PaymentReconciliationOptions _options;
    private readonly ILogger<PaymentReconciliationWorker> _logger;
    private readonly TimeProvider _timeProvider;

    public PaymentReconciliationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<PaymentReconciliationOptions> options,
        ILogger<PaymentReconciliationWorker> logger,
        TimeProvider? timeProvider = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Payment reconciliation background worker is disabled.");
            return;
        }

        _logger.LogInformation(
            "Payment reconciliation background worker started. Runs every {IntervalMinutes} minutes for orders older than {ThresholdMinutes} minutes.",
            _options.IntervalMinutes,
            _options.PendingThresholdMinutes);

        // Initial delay to let startup and migrations complete
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcilePendingOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during payment reconciliation pass.");
            }

            var delay = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("Payment reconciliation background worker stopped.");
    }

    private async Task ReconcilePendingOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var paymentRepo = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var gateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();
        var webhookStore = scope.ServiceProvider.GetRequiredService<IPaymentWebhookStore>();
        var notificationService = scope.ServiceProvider.GetService<IOrderNotificationService>();
        var clientRepo = scope.ServiceProvider.GetService<IClientRepository>();
        var catalogRepo = scope.ServiceProvider.GetService<ICatalogRepository>();

        var threshold = _timeProvider.GetUtcNow().AddMinutes(-Math.Max(1, _options.PendingThresholdMinutes));
        var pendingOrders = await orderRepo.GetPendingOrdersOlderThanAsync(threshold, cancellationToken);

        if (pendingOrders.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Checking {Count} pending orders for payment reconciliation.", pendingOrders.Count);

        foreach (var order in pendingOrders)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var payments = await paymentRepo.GetPaymentsByOrderIdAsync(order.Id, cancellationToken);
                foreach (var payment in payments)
                {
                    if (string.IsNullOrWhiteSpace(payment.GatewayPaymentId))
                    {
                        continue;
                    }

                    var providerPayment = await gateway.GetPaymentAsync(payment.GatewayPaymentId, cancellationToken);
                    if (providerPayment is null || string.IsNullOrWhiteSpace(providerPayment.Status))
                    {
                        continue;
                    }

                    var mapping = MercadoPagoPaymentStatusMapper.Map(providerPayment.Status);
                    if (mapping.TargetOrderStatus == OrderStatus.Paid)
                    {
                        _logger.LogInformation(
                            "Reconciliation: Order {OrderNumber} payment {PaymentId} was approved on provider.",
                            order.OrderNumber,
                            payment.GatewayPaymentId);

                        var result = await webhookStore.ApplyVerifiedPaymentAsync(
                            new VerifiedPaymentPersistenceCommand(
                                order.Id,
                                order.ExternalReference,
                                gateway.GatewayName,
                                payment.GatewayPaymentId,
                                payment.PreferenceId,
                                providerPayment.Amount,
                                providerPayment.Currency ?? order.Currency,
                                mapping.NormalizedStatus,
                                providerPayment.StatusDetail,
                                "reconciliation-worker",
                                "{\"reconciled\":true}",
                                mapping.TargetOrderStatus,
                                _timeProvider.GetUtcNow()),
                            cancellationToken);

                        if (result.OrderStatus == OrderStatus.Paid && notificationService is not null)
                        {
                            var client = clientRepo != null ? await clientRepo.GetByIdAsync(order.ClientId, cancellationToken) : null;
                            var package = catalogRepo != null ? await catalogRepo.GetActivePackageByIdAsync(order.PackageId, cancellationToken) : null;

                            if (client != null && !string.IsNullOrWhiteSpace(client.Email))
                            {
                                await notificationService.NotifyOrderPaidAsync(
                                    new OrderPaidNotification(
                                        order.Id,
                                        order.OrderNumber,
                                        order.PackageId,
                                        package?.Name ?? "Servicio Digital",
                                        order.TotalAmount,
                                        order.Currency,
                                        client.FullName,
                                        client.Email,
                                        client.PhoneNumber,
                                        client.CompanyName,
                                        _timeProvider.GetUtcNow()),
                                    cancellationToken);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reconcile order {OrderNumber}.", order.OrderNumber);
            }
        }
    }
}
