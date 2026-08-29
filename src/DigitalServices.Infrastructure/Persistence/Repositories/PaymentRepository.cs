using DigitalServices.Application.Abstractions.Persistence;
using DigitalServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalServices.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(ApplicationDbContext dbContext) : IPaymentRepository
{
    public Task<Payment?> GetByPreferenceIdAsync(
        string gateway,
        string preferenceId,
        CancellationToken cancellationToken)
    {
        return dbContext.Payments
            .SingleOrDefaultAsync(
                payment => payment.Gateway == gateway && payment.PreferenceId == preferenceId,
                cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        await dbContext.Payments.AddAsync(payment, cancellationToken);
    }
}
