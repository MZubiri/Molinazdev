using DigitalServices.Application.Abstractions.Persistence;
using DigitalServices.Domain.Entities;

namespace DigitalServices.Application.Catalog;

public sealed class CatalogService(ICatalogRepository catalogRepository) : ICatalogService
{
    public async Task<IReadOnlyList<ServiceCatalogDto>> GetActiveServicesAsync(
        CancellationToken cancellationToken)
    {
        var services = await catalogRepository.GetActiveServicesAsync(cancellationToken);
        return services.Select(MapService).ToArray();
    }

    public async Task<ServiceCatalogDto?> GetActiveServiceBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var service = await catalogRepository.GetActiveServiceBySlugAsync(
            slug.Trim().ToLowerInvariant(),
            cancellationToken);

        return service is null ? null : MapService(service);
    }

    private static ServiceCatalogDto MapService(Service service)
    {
        var packages = service.Packages
            .Where(static package => package.IsActive)
            .OrderBy(static package => package.Price)
            .Select(static package => new ServicePackageDto(
                package.Id,
                package.ServiceId,
                package.Name,
                package.Description,
                package.Price,
                package.Currency,
                package.DeliveryDays,
                package.Features.ToArray()))
            .ToArray();

        return new ServiceCatalogDto(
            service.Id,
            service.Title,
            service.Slug,
            service.Description,
            service.IconUrl,
            packages);
    }
}
