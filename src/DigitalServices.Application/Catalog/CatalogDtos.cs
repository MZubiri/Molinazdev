namespace DigitalServices.Application.Catalog;

public sealed record ServiceCatalogDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string? IconUrl,
    IReadOnlyList<ServicePackageDto> Packages);

public sealed record ServicePackageDto(
    Guid Id,
    Guid ServiceId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int DeliveryDays,
    IReadOnlyList<string> Features);
