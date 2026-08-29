using DigitalServices.Domain.Common;
using DigitalServices.Domain.Enums;

namespace DigitalServices.Domain.Entities;

public sealed class Order
{
    private const string ExternalReferencePrefix = "order:";
    private readonly List<Payment> _payments = [];

    private Order()
    {
    }

    private Order(
        Guid id,
        Guid clientId,
        Guid packageId,
        decimal totalAmount,
        string currency,
        DateTimeOffset createdAt)
    {
        Id = DomainGuard.RequiredId(id, nameof(id));
        ClientId = DomainGuard.RequiredId(clientId, nameof(clientId));
        PackageId = DomainGuard.RequiredId(packageId, nameof(packageId));
        TotalAmount = DomainGuard.PositiveAmount(totalAmount, nameof(totalAmount));
        Currency = DomainGuard.NormalizeCurrency(currency, nameof(currency));
        CreatedAt = DomainGuard.UtcTimestamp(createdAt, nameof(createdAt));
        UpdatedAt = CreatedAt;
        OrderNumber = BuildOrderNumber(Id, CreatedAt);
        ExternalReference = BuildExternalReference(Id);
        Status = OrderStatus.Pending;
    }

    public Guid Id { get; private set; }

    public string OrderNumber { get; private set; } = string.Empty;

    public Guid ClientId { get; private set; }

    public Guid PackageId { get; private set; }

    /// <summary>
    /// Immutable price snapshot captured when the order is created.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    /// <summary>
    /// Stable provider-facing reference derived from the complete internal order identifier.
    /// </summary>
    public string ExternalReference { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? PaidAt { get; private set; }

    public Client Client { get; private set; } = null!;

    public ServicePackage Package { get; private set; } = null!;

    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    public static Order Create(
        Guid clientId,
        Guid packageId,
        decimal totalAmount,
        string currency,
        DateTimeOffset createdAt)
    {
        return new Order(
            Guid.NewGuid(),
            clientId,
            packageId,
            totalAmount,
            currency,
            createdAt);
    }

    public static string BuildExternalReference(Guid orderId)
    {
        DomainGuard.RequiredId(orderId, nameof(orderId));
        return $"{ExternalReferencePrefix}{orderId:N}";
    }

    public static bool TryParseExternalReference(string? externalReference, out Guid orderId)
    {
        orderId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(externalReference) ||
            !externalReference.StartsWith(ExternalReferencePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return Guid.TryParseExact(externalReference.AsSpan(ExternalReferencePrefix.Length), "N", out orderId);
    }

    public bool CanTransitionTo(OrderStatus targetStatus)
    {
        if (Status == targetStatus)
        {
            return true;
        }

        return Status switch
        {
            OrderStatus.Pending => targetStatus is OrderStatus.Paid or OrderStatus.Cancelled,
            OrderStatus.Paid => targetStatus is OrderStatus.InDevelopment,
            OrderStatus.InDevelopment => targetStatus is OrderStatus.Completed,
            OrderStatus.Completed => false,
            OrderStatus.Cancelled => false,
            _ => false
        };
    }

    /// <summary>
    /// Applies a valid transition and returns whether state changed. Repeated notifications are no-ops.
    /// </summary>
    public bool TryTransitionTo(OrderStatus targetStatus, DateTimeOffset occurredAt)
    {
        if (Status == targetStatus || !CanTransitionTo(targetStatus))
        {
            return false;
        }

        ApplyTransition(targetStatus, occurredAt);
        return true;
    }

    public void TransitionTo(OrderStatus targetStatus, DateTimeOffset occurredAt)
    {
        if (Status == targetStatus)
        {
            return;
        }

        if (!CanTransitionTo(targetStatus))
        {
            throw new InvalidOperationException($"Order cannot transition from {Status} to {targetStatus}.");
        }

        ApplyTransition(targetStatus, occurredAt);
    }

    private static string BuildOrderNumber(Guid orderId, DateTimeOffset createdAt)
    {
        var suffix = orderId.ToString("N")[..12].ToUpperInvariant();
        return $"ORD-{createdAt.Year:D4}-{suffix}";
    }

    private void ApplyTransition(OrderStatus targetStatus, DateTimeOffset occurredAt)
    {
        var timestamp = DomainGuard.UtcTimestamp(occurredAt, nameof(occurredAt));
        Status = targetStatus;
        UpdatedAt = timestamp;

        if (targetStatus == OrderStatus.Paid && PaidAt is null)
        {
            PaidAt = timestamp;
        }
    }
}
