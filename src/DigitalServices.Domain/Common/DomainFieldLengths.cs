namespace DigitalServices.Domain.Common;

/// <summary>
/// Shared persistence-agnostic limits for domain fields.
/// Infrastructure mappings should use the same limits.
/// </summary>
public static class DomainFieldLengths
{
    public const int ServiceTitle = 160;
    public const int Slug = 180;
    public const int Description = 4_000;
    public const int Url = 2_048;
    public const int PackageName = 160;
    public const int Feature = 500;
    public const int FullName = 200;
    public const int Email = 320;
    public const int PhoneNumber = 50;
    public const int CompanyName = 200;
    public const int Currency = 3;
    public const int OrderNumber = 32;
    public const int ExternalReference = 64;
    public const int PaymentGateway = 50;
    public const int GatewayIdentifier = 128;
    public const int PaymentStatus = 64;
    public const int PaymentStatusDetail = 255;
}
