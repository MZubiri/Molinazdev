using DigitalServices.Application.Abstractions.Persistence;
using DigitalServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalServices.Infrastructure.Persistence.Repositories;

public sealed class ClientRepository(ApplicationDbContext dbContext) : IClientRepository
{
    public Task<Client?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return dbContext.Clients.SingleOrDefaultAsync(
            client => client.Email == normalizedEmail,
            cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken)
    {
        await dbContext.Clients.AddAsync(client, cancellationToken);
    }
}
