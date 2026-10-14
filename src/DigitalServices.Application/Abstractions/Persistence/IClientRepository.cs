using DigitalServices.Domain.Entities;

namespace DigitalServices.Application.Abstractions.Persistence;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Client?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task AddAsync(Client client, CancellationToken cancellationToken);
}
