using DigitalServices.Application.Abstractions.Persistence;
using DigitalServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalServices.Infrastructure.Persistence.Repositories;

public sealed class CatalogRepository(ApplicationDbContext dbContext) : ICatalogRepository
{
    public async Task<IReadOnlyList<Service>> GetActiveServicesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Services
            .AsNoTracking()
            .Where(service => service.IsActive)
            .Include(service => service.Packages.Where(package => package.IsActive))
            .AsSplitQuery()
            .OrderBy(service => service.Title)
            .ToListAsync(cancellationToken);
    }

    public Task<Service?> GetActiveServiceBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        return dbContext.Services
            .AsNoTracking()
            .Where(service => service.IsActive && service.Slug == slug)
            .Include(service => service.Packages.Where(package => package.IsActive))
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<ServicePackage?> GetActivePackageByIdAsync(
        Guid packageId,
        CancellationToken cancellationToken)
    {
        return dbContext.ServicePackages
            .Include(package => package.Service)
            .SingleOrDefaultAsync(
                package => package.Id == packageId && package.IsActive && package.Service.IsActive,
                cancellationToken);
    }
}
