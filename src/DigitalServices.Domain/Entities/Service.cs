using DigitalServices.Domain.Common;

namespace DigitalServices.Domain.Entities;

public sealed class Service
{
    private readonly List<ServicePackage> _packages = [];

    private Service()
    {
    }

    private Service(
        Guid id,
        string title,
        string slug,
        string? description,
        string? iconUrl,
        bool isActive,
        DateTimeOffset createdAt)
    {
        Id = DomainGuard.RequiredId(id, nameof(id));
        Title = DomainGuard.Required(title, DomainFieldLengths.ServiceTitle, nameof(title));
        Slug = DomainGuard.NormalizeSlug(slug, nameof(slug));
        Description = DomainGuard.Optional(description, DomainFieldLengths.Description, nameof(description));
        IconUrl = DomainGuard.Optional(iconUrl, DomainFieldLengths.Url, nameof(iconUrl));
        IsActive = isActive;
        CreatedAt = DomainGuard.UtcTimestamp(createdAt, nameof(createdAt));
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? IconUrl { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<ServicePackage> Packages => _packages.AsReadOnly();

    public static Service Create(
        string title,
        string slug,
        string? description,
        string? iconUrl,
        DateTimeOffset createdAt,
        bool isActive = true)
    {
        return new Service(Guid.NewGuid(), title, slug, description, iconUrl, isActive, createdAt);
    }

    public void UpdateDetails(
        string title,
        string slug,
        string? description,
        string? iconUrl,
        DateTimeOffset updatedAt)
    {
        Title = DomainGuard.Required(title, DomainFieldLengths.ServiceTitle, nameof(title));
        Slug = DomainGuard.NormalizeSlug(slug, nameof(slug));
        Description = DomainGuard.Optional(description, DomainFieldLengths.Description, nameof(description));
        IconUrl = DomainGuard.Optional(iconUrl, DomainFieldLengths.Url, nameof(iconUrl));
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
}
