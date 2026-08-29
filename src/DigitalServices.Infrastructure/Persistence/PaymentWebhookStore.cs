using System.Data;
using DigitalServices.Application.Common;
using DigitalServices.Application.Payments;
using DigitalServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace DigitalServices.Infrastructure.Persistence;

public sealed class PaymentWebhookStore(
    ApplicationDbContext dbContext,
    ILogger<PaymentWebhookStore> logger) : IPaymentWebhookStore
{
    public Task<bool> HasProcessedNotificationAsync(
        string gateway,
        string notificationId,
        CancellationToken cancellationToken)
    {
        return dbContext.PaymentNotificationReceipts
            .AsNoTracking()
            .AnyAsync(
                receipt => receipt.Gateway == gateway && receipt.NotificationId == notificationId,
                cancellationToken);
    }

    public async Task<VerifiedPaymentPersistenceResult> ApplyVerifiedPaymentAsync(
        VerifiedPaymentPersistenceCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            // The row lock serializes all payment notifications that target the same internal order.
            var order = await dbContext.Orders
                .FromSqlInterpolated($"SELECT * FROM `Orders` WHERE `Id` = {command.OrderId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);

            if (order is null)
            {
                throw new ResourceNotFoundException("Order", command.OrderId);
            }

            EnsureMatchesOrder(order, command);

            if (!string.IsNullOrWhiteSpace(command.NotificationId))
            {
                var notificationReceipt = await dbContext.PaymentNotificationReceipts
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        receipt => receipt.Gateway == command.Gateway &&
                                   receipt.NotificationId == command.NotificationId,
                        cancellationToken);

                if (notificationReceipt is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return new VerifiedPaymentPersistenceResult(
                        VerifiedPaymentPersistenceOutcome.DuplicateNotification,
                        notificationReceipt.PaymentId,
                        order.Status,
                        OrderStatusChanged: false);
                }
            }

            var payment = await dbContext.Payments.SingleOrDefaultAsync(
                candidate => candidate.Gateway == command.Gateway &&
                             candidate.GatewayPaymentId == command.GatewayPaymentId,
                cancellationToken);

            if (payment is not null && payment.OrderId != order.Id)
            {
                throw new InvalidOperationException(
                    "The gateway payment identifier is already associated with another order.");
            }

            var unchangedGatewayPayment = payment is not null &&
                string.Equals(payment.Status, command.Status, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(payment.StatusDetail, command.StatusDetail, StringComparison.Ordinal) &&
                string.Equals(payment.NotificationId, command.NotificationId, StringComparison.Ordinal);

            if (payment is null)
            {
                payment = await FindReusablePreferencePaymentAsync(command, cancellationToken);
            }

            if (payment is null)
            {
                payment = Payment.CreateVerified(
                    order.Id,
                    command.Gateway,
                    command.GatewayPaymentId,
                    command.PreferenceId,
                    command.Amount,
                    command.Currency,
                    command.Status,
                    command.StatusDetail,
                    command.NotificationId,
                    command.RawPayload,
                    command.ProcessedAt);

                await dbContext.Payments.AddAsync(payment, cancellationToken);
            }
            else
            {
                payment.ApplyVerifiedState(
                    command.GatewayPaymentId,
                    command.PreferenceId,
                    command.Amount,
                    command.Currency,
                    command.Status,
                    command.StatusDetail,
                    command.NotificationId,
                    command.RawPayload,
                    command.ProcessedAt);
            }

            var orderStatusChanged = command.TargetOrderStatus is { } targetStatus &&
                order.TryTransitionTo(targetStatus, command.ProcessedAt);

            if (!string.IsNullOrWhiteSpace(command.NotificationId))
            {
                await dbContext.PaymentNotificationReceipts.AddAsync(
                    PaymentNotificationReceipt.Create(
                        command.Gateway,
                        command.NotificationId,
                        command.GatewayPaymentId,
                        payment.Id,
                        command.ProcessedAt),
                    cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new VerifiedPaymentPersistenceResult(
                unchangedGatewayPayment
                    ? VerifiedPaymentPersistenceOutcome.DuplicateGatewayPayment
                    : VerifiedPaymentPersistenceOutcome.Applied,
                payment.Id,
                order.Status,
                orderStatusChanged);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();

            logger.LogInformation(
                "A concurrent duplicate was detected for gateway payment {GatewayPaymentId}.",
                command.GatewayPaymentId);

            var existing = await dbContext.Payments
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    payment => payment.Gateway == command.Gateway &&
                               (payment.GatewayPaymentId == command.GatewayPaymentId ||
                                (command.NotificationId != null &&
                                 payment.NotificationId == command.NotificationId)),
                    cancellationToken);

            var orderStatus = await dbContext.Orders
                .AsNoTracking()
                .Where(order => order.Id == command.OrderId)
                .Select(order => order.Status)
                .SingleAsync(cancellationToken);

            if (existing is null)
            {
                throw;
            }

            return new VerifiedPaymentPersistenceResult(
                VerifiedPaymentPersistenceOutcome.DuplicateGatewayPayment,
                existing.Id,
                orderStatus,
                OrderStatusChanged: false);
        }
    }

    private Task<Payment?> FindReusablePreferencePaymentAsync(
        VerifiedPaymentPersistenceCommand command,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Payments.Where(
            payment => payment.OrderId == command.OrderId &&
                       payment.Gateway == command.Gateway &&
                       payment.GatewayPaymentId == null);

        if (!string.IsNullOrWhiteSpace(command.PreferenceId))
        {
            query = query.Where(payment => payment.PreferenceId == command.PreferenceId);
        }

        return query.OrderBy(payment => payment.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    }

    private static void EnsureMatchesOrder(
        Order order,
        VerifiedPaymentPersistenceCommand command)
    {
        if (!string.Equals(order.ExternalReference, command.ExpectedExternalReference, StringComparison.Ordinal) ||
            order.TotalAmount != command.Amount ||
            !string.Equals(order.Currency, command.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The verified payment no longer matches the internal order snapshot.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is MySqlException { Number: 1062 };
    }
}
