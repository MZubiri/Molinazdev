using DigitalServices.Application.Abstractions.Persistence;
using DigitalServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalServices.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(ApplicationDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                order => order.Id == id,
                cancellationToken);
    }

    public Task<Order?> GetByExternalReferenceAsync(
        string externalReference,
        CancellationToken cancellationToken)
    {
        return dbContext.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                order => order.ExternalReference == externalReference,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetPendingOrdersOlderThanAsync(
        DateTimeOffset threshold,
        CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Status == Domain.Enums.OrderStatus.Pending && order.CreatedAt <= threshold)
            .OrderBy(order => order.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await dbContext.Orders.AddAsync(order, cancellationToken);
    }
}
