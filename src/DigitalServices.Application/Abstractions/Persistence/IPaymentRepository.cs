using DigitalServices.Domain.Entities;

namespace DigitalServices.Application.Abstractions.Persistence;

public interface IPaymentRepository
{
    Task<Payment?> GetByPreferenceIdAsync(
        string gateway,
        string preferenceId,
        CancellationToken cancellationToken);

    Task AddAsync(Payment payment, CancellationToken cancellationToken);
}
