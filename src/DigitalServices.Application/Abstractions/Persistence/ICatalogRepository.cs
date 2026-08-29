using DigitalServices.Domain.Entities;

namespace DigitalServices.Application.Abstractions.Persistence;

public interface ICatalogRepository
{
    Task<IReadOnlyList<Service>> GetActiveServicesAsync(CancellationToken cancellationToken);

    Task<Service?> GetActiveServiceBySlugAsync(string slug, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a package only when both it and its owning service are active.
    /// </summary>
    Task<ServicePackage?> GetActivePackageByIdAsync(Guid packageId, CancellationToken cancellationToken);
}
