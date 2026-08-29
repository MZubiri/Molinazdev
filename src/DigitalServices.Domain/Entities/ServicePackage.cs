using DigitalServices.Domain.Common;

namespace DigitalServices.Domain.Entities;

public sealed class ServicePackage
{
    private ServicePackage()
    {
    }

    private ServicePackage(
        Guid id,
        Guid serviceId,
        string name,
        string? description,
        decimal price,
        string currency,
        int deliveryDays,
        IEnumerable<string>? features,
        bool isActive,
        DateTimeOffset createdAt)
    {
        Id = DomainGuard.RequiredId(id, nameof(id));
        ServiceId = DomainGuard.RequiredId(serviceId, nameof(serviceId));
        Name = DomainGuard.Required(name, DomainFieldLengths.PackageName, nameof(name));
        Description = DomainGuard.Optional(description, DomainFieldLengths.Description, nameof(description));
        Price = DomainGuard.PositiveAmount(price, nameof(price));
        Currency = DomainGuard.NormalizeCurrency(currency, nameof(currency));
        DeliveryDays = ValidateDeliveryDays(deliveryDays);
        Features = NormalizeFeatures(features);
        IsActive = isActive;
        CreatedAt = DomainGuard.UtcTimestamp(createdAt, nameof(createdAt));
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid ServiceId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public int DeliveryDays { get; private set; }

    public IReadOnlyList<string> Features { get; private set; } = Array.Empty<string>();

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Service Service { get; private set; } = null!;

    public static ServicePackage Create(
        Guid serviceId,
        string name,
        string? description,
        decimal price,
        string currency,
        int deliveryDays,
        IEnumerable<string>? features,
        DateTimeOffset createdAt,
        bool isActive = true)
    {
        return new ServicePackage(
            Guid.NewGuid(),
            serviceId,
            name,
            description,
            price,
            currency,
            deliveryDays,
            features,
            isActive,
            createdAt);
    }

    public void UpdateDetails(
        string name,
        string? description,
        decimal price,
        string currency,
        int deliveryDays,
        IEnumerable<string>? features,
        DateTimeOffset updatedAt)
    {
        Name = DomainGuard.Required(name, DomainFieldLengths.PackageName, nameof(name));
        Description = DomainGuard.Optional(description, DomainFieldLengths.Description, nameof(description));
        Price = DomainGuard.PositiveAmount(price, nameof(price));
        Currency = DomainGuard.NormalizeCurrency(currency, nameof(currency));
        DeliveryDays = ValidateDeliveryDays(deliveryDays);
        Features = NormalizeFeatures(features);
        UpdatedAt = DomainGuard.UtcTimestamp(updatedAt, nameof(updatedAt));
    }

    public void SetActive(bool isActive, DateTimeOffset updatedAt)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        UpdatedAt = DomainGuard.UtcTimestamp(updatedAt, nameof(updatedAt));
    }

    private static int ValidateDeliveryDays(int deliveryDays)
    {
        if (deliveryDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryDays), "Delivery days must be greater than zero.");
        }

        return deliveryDays;
    }

    private static IReadOnlyList<string> NormalizeFeatures(IEnumerable<string>? features)
    {
        if (features is null)
        {
            return Array.Empty<string>();
        }

        return features
            .Where(static feature => !string.IsNullOrWhiteSpace(feature))
            .Select(static feature => DomainGuard.Required(
                feature,
                DomainFieldLengths.Feature,
                nameof(features)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
