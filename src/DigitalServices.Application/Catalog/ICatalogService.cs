namespace DigitalServices.Application.Catalog;

public interface ICatalogService
{
    Task<IReadOnlyList<ServiceCatalogDto>> GetActiveServicesAsync(CancellationToken cancellationToken);

    Task<ServiceCatalogDto?> GetActiveServiceBySlugAsync(
        string slug,
        CancellationToken cancellationToken);
}
