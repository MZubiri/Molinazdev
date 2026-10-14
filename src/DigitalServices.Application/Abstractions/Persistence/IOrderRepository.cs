using DigitalServices.Domain.Entities;

namespace DigitalServices.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<Order?> GetByExternalReferenceAsync(
        string externalReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetPendingOrdersOlderThanAsync(
        DateTimeOffset threshold,
        CancellationToken cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);
}
