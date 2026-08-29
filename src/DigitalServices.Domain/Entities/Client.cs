using DigitalServices.Domain.Common;

namespace DigitalServices.Domain.Entities;

public sealed class Client
{
    private readonly List<Order> _orders = [];

    private Client()
    {
    }

    private Client(
        Guid id,
        string fullName,
        string email,
        string? phoneNumber,
        string? companyName,
        DateTimeOffset createdAt)
    {
        Id = DomainGuard.RequiredId(id, nameof(id));
        ApplyContactDetails(fullName, email, phoneNumber, companyName);
        CreatedAt = DomainGuard.UtcTimestamp(createdAt, nameof(createdAt));
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    /// <summary>
    /// Canonical lowercase email used for both display and comparisons.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public string? CompanyName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    public static Client Create(
        string fullName,
        string email,
        string? phoneNumber,
        string? companyName,
        DateTimeOffset createdAt)
    {
        return new Client(Guid.NewGuid(), fullName, email, phoneNumber, companyName, createdAt);
    }

    public static string NormalizeEmail(string email)
    {
        return DomainGuard.NormalizeEmail(email, nameof(email));
    }

    public void UpdateContactDetails(
        string fullName,
        string email,
        string? phoneNumber,
        string? companyName,
        DateTimeOffset updatedAt)
    {
        ApplyContactDetails(fullName, email, phoneNumber, companyName);
        UpdatedAt = DomainGuard.UtcTimestamp(updatedAt, nameof(updatedAt));
    }

    private void ApplyContactDetails(
        string fullName,
        string email,
        string? phoneNumber,
        string? companyName)
    {
        FullName = DomainGuard.Required(fullName, DomainFieldLengths.FullName, nameof(fullName));
        Email = DomainGuard.NormalizeEmail(email, nameof(email));
        PhoneNumber = DomainGuard.Optional(phoneNumber, DomainFieldLengths.PhoneNumber, nameof(phoneNumber));
        CompanyName = DomainGuard.Optional(companyName, DomainFieldLengths.CompanyName, nameof(companyName));
    }
}
