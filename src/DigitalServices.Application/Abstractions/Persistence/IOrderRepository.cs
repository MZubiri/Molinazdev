using DigitalServices.Domain.Entities;

namespace DigitalServices.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task<Order?> GetByExternalReferenceAsync(
        string externalReference,
        CancellationToken cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);
}
