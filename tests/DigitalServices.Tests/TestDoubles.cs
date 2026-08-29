using System.Collections.Concurrent;
using DigitalServices.Application.Abstractions.Persistence;
using DigitalServices.Application.Payments;
using DigitalServices.Domain.Entities;

namespace DigitalServices.Tests;

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

internal sealed class TestCatalogRepository : ICatalogRepository
{
    public ServicePackage? ActivePackage { get; init; }

    public IReadOnlyList<Service> ActiveServices { get; init; } = Array.Empty<Service>();

    public Task<IReadOnlyList<Service>> GetActiveServicesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ActiveServices);
    }

    public Task<Service?> GetActiveServiceBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ActiveServices.SingleOrDefault(service => service.Slug == slug));
    }

    public Task<ServicePackage?> GetActivePackageByIdAsync(
        Guid packageId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ActivePackage?.Id == packageId ? ActivePackage : null);
    }
}

internal sealed class InMemoryClientRepository : IClientRepository
{
    private readonly List<Client> _clients = [];

    public IReadOnlyList<Client> Clients => _clients;

    public Task<Client?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            _clients.SingleOrDefault(client => client.Email == normalizedEmail));
    }

    public Task AddAsync(Client client, CancellationToken cancellationToken)
    {
        _clients.Add(client);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<string, Order> _orders = new(StringComparer.Ordinal);

    public IReadOnlyCollection<Order> Orders => _orders.Values.ToArray();

    public void Seed(Order order)
    {
        _orders[order.ExternalReference] = order;
    }

    public Task<Order?> GetByExternalReferenceAsync(
        string externalReference,
        CancellationToken cancellationToken)
    {
        _orders.TryGetValue(externalReference, out var order);
        return Task.FromResult(order);
    }

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        if (!_orders.TryAdd(order.ExternalReference, order))
        {
            throw new InvalidOperationException("Duplicate external reference in test store.");
        }

        return Task.CompletedTask;
    }
}

internal sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly List<Payment> _payments = [];

    public IReadOnlyList<Payment> Payments => _payments;

    public Task<Payment?> GetByPreferenceIdAsync(
        string gateway,
        string preferenceId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            _payments.FirstOrDefault(payment =>
                payment.Gateway == gateway && payment.PreferenceId == preferenceId));
    }

    public Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        _payments.Add(payment);
        return Task.CompletedTask;
    }
}

internal sealed class CountingUnitOfWork : IUnitOfWork
{
    private int _saveCount;

    public int SaveCount => Volatile.Read(ref _saveCount);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _saveCount);
        return Task.FromResult(1);
    }
}

internal sealed class TestPaymentGateway : IPaymentGateway
{
    private int _createPreferenceCalls;
    private int _getPaymentCalls;

    public string GatewayName { get; init; } = "MercadoPago";

    public CreatePaymentPreferenceRequest? LastPreferenceRequest { get; private set; }

    public CreatePaymentPreferenceResult PreferenceResult { get; init; } = new(
        "pref-test-001",
        new Uri("https://sandbox.mercadopago.test/checkout"));

    public Func<string, GatewayPaymentDetails?> GetPaymentHandler { get; set; } = _ => null;

    public TimeSpan GetPaymentDelay { get; init; }

    public int CreatePreferenceCalls => Volatile.Read(ref _createPreferenceCalls);

    public int GetPaymentCalls => Volatile.Read(ref _getPaymentCalls);

    public Task<CreatePaymentPreferenceResult> CreatePreferenceAsync(
        CreatePaymentPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _createPreferenceCalls);
        LastPreferenceRequest = request;
        return Task.FromResult(PreferenceResult);
    }

    public async Task<GatewayPaymentDetails?> GetPaymentAsync(
        string gatewayPaymentId,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _getPaymentCalls);
        if (GetPaymentDelay > TimeSpan.Zero)
        {
            await Task.Delay(GetPaymentDelay, cancellationToken);
        }

        return GetPaymentHandler(gatewayPaymentId);
    }
}

internal sealed class ConstantSignatureValidator(bool isValid) : IPaymentWebhookSignatureValidator
{
    private int _calls;

    public int Calls => Volatile.Read(ref _calls);

    public bool IsValid(WebhookSignatureValidationRequest request)
    {
        Interlocked.Increment(ref _calls);
        return isValid;
    }
}

/// <summary>
/// Models the atomic database contract using one lock and unique notification/payment keys.
/// It intentionally supports updates of an existing gateway payment (for pending to approved).
/// </summary>
internal sealed class ThreadSafePaymentWebhookStore : IPaymentWebhookStore
{
    private readonly object _gate = new();
    private readonly IReadOnlyDictionary<Guid, Order> _orders;
    private readonly Dictionary<(string Gateway, string GatewayPaymentId), StoredPayment> _payments = [];
    private readonly Dictionary<(string Gateway, string NotificationId), Guid> _notifications = [];
    private int _applyCalls;

    public ThreadSafePaymentWebhookStore(params Order[] orders)
    {
        _orders = orders.ToDictionary(order => order.Id);
    }

    public int ApplyCalls => Volatile.Read(ref _applyCalls);

    public int PaymentCount
    {
        get
        {
            lock (_gate)
            {
                return _payments.Count;
            }
        }
    }

    public string? GetStoredStatus(string gateway, string gatewayPaymentId)
    {
        lock (_gate)
        {
            return _payments.TryGetValue((gateway, gatewayPaymentId), out var payment)
                ? payment.Status
                : null;
        }
    }

    public Task<bool> HasProcessedNotificationAsync(
        string gateway,
        string notificationId,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_notifications.ContainsKey((gateway, notificationId)));
        }
    }

    public Task<VerifiedPaymentPersistenceResult> ApplyVerifiedPaymentAsync(
        VerifiedPaymentPersistenceCommand command,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _applyCalls);

        lock (_gate)
        {
            var order = _orders[command.OrderId];
            if (order.ExternalReference != command.ExpectedExternalReference ||
                order.TotalAmount != command.Amount ||
                order.Currency != command.Currency)
            {
                throw new InvalidOperationException("Verified facts do not match the order.");
            }

            if (command.NotificationId is not null &&
                _notifications.TryGetValue(
                    (command.Gateway, command.NotificationId),
                    out var notificationPaymentId))
            {
                return Task.FromResult(new VerifiedPaymentPersistenceResult(
                    VerifiedPaymentPersistenceOutcome.DuplicateNotification,
                    notificationPaymentId,
                    order.Status,
                    OrderStatusChanged: false));
            }

            var paymentKey = (command.Gateway, command.GatewayPaymentId);
            var alreadyExists = _payments.TryGetValue(paymentKey, out var storedPayment);
            storedPayment ??= new StoredPayment(Guid.NewGuid(), command.Status, command.StatusDetail);

            var unchanged = alreadyExists &&
                string.Equals(storedPayment.Status, command.Status, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(storedPayment.StatusDetail, command.StatusDetail, StringComparison.Ordinal);

            storedPayment.Status = command.Status;
            storedPayment.StatusDetail = command.StatusDetail;
            _payments[paymentKey] = storedPayment;

            if (command.NotificationId is not null)
            {
                _notifications[(command.Gateway, command.NotificationId)] = storedPayment.Id;
            }

            var changed = command.TargetOrderStatus is { } target &&
                order.TryTransitionTo(target, command.ProcessedAt);

            return Task.FromResult(new VerifiedPaymentPersistenceResult(
                unchanged
                    ? VerifiedPaymentPersistenceOutcome.DuplicateGatewayPayment
                    : VerifiedPaymentPersistenceOutcome.Applied,
                storedPayment.Id,
                order.Status,
                changed));
        }
    }

    private sealed class StoredPayment(Guid id, string status, string? statusDetail)
    {
        public Guid Id { get; } = id;

        public string Status { get; set; } = status;

        public string? StatusDetail { get; set; } = statusDetail;
    }
}
