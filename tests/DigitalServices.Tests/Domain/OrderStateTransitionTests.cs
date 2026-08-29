using DigitalServices.Application.Payments;
using DigitalServices.Domain.Entities;
using DigitalServices.Domain.Enums;

namespace DigitalServices.Tests.Domain;

public sealed class OrderStateTransitionTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TransitionTo_HappyPath_AdvancesWithoutChangingPaidTimestamp()
    {
        var order = CreateOrder();
        var paidAt = CreatedAt.AddMinutes(1);

        order.TransitionTo(OrderStatus.Paid, paidAt);
        order.TransitionTo(OrderStatus.InDevelopment, CreatedAt.AddDays(1));
        order.TransitionTo(OrderStatus.Completed, CreatedAt.AddDays(5));

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(paidAt, order.PaidAt);
        Assert.Equal(CreatedAt.AddDays(5), order.UpdatedAt);
    }

    [Fact]
    public void TryTransitionTo_OldPendingNotification_DoesNotDegradeCompletedOrder()
    {
        var order = CreateCompletedOrder();
        var mappedStatus = MercadoPagoPaymentStatusMapper.MapToOrderStatus("pending");

        var changed = order.TryTransitionTo(mappedStatus!.Value, CreatedAt.AddDays(10));

        Assert.False(changed);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(CreatedAt.AddDays(5), order.UpdatedAt);
    }

    [Fact]
    public void TryTransitionTo_RejectedNotification_DoesNotDegradePaidOrder()
    {
        var order = CreateOrder();
        order.TransitionTo(OrderStatus.Paid, CreatedAt.AddHours(1));
        var mappedStatus = MercadoPagoPaymentStatusMapper.MapToOrderStatus("rejected");

        var changed = order.TryTransitionTo(mappedStatus!.Value, CreatedAt.AddHours(2));

        Assert.False(changed);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.InDevelopment)]
    [InlineData(OrderStatus.Completed)]
    public void TryTransitionTo_Cancelled_DoesNotCancelAnOrderAfterPayment(OrderStatus currentStatus)
    {
        var order = CreateOrderAtState(currentStatus);

        var changed = order.TryTransitionTo(OrderStatus.Cancelled, CreatedAt.AddDays(20));

        Assert.False(changed);
        Assert.Equal(currentStatus, order.Status);
    }

    [Fact]
    public void TransitionTo_InvalidTransition_ThrowsAndPreservesState()
    {
        var order = CreateCompletedOrder();

        Assert.Throws<InvalidOperationException>(
            () => order.TransitionTo(OrderStatus.Paid, CreatedAt.AddDays(6)));
        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public void TryTransitionTo_DuplicateApprovedNotification_IsIdempotent()
    {
        var order = CreateOrder();
        var firstProcessedAt = CreatedAt.AddMinutes(1);

        Assert.True(order.TryTransitionTo(OrderStatus.Paid, firstProcessedAt));
        Assert.False(order.TryTransitionTo(OrderStatus.Paid, CreatedAt.AddMinutes(2)));

        Assert.Equal(firstProcessedAt, order.PaidAt);
        Assert.Equal(firstProcessedAt, order.UpdatedAt);
    }

    private static Order CreateOrder()
    {
        return Order.Create(Guid.NewGuid(), Guid.NewGuid(), 499.90m, "PEN", CreatedAt);
    }

    private static Order CreateCompletedOrder()
    {
        return CreateOrderAtState(OrderStatus.Completed);
    }

    private static Order CreateOrderAtState(OrderStatus targetStatus)
    {
        var order = CreateOrder();
        if (targetStatus >= OrderStatus.Paid && targetStatus != OrderStatus.Cancelled)
        {
            order.TransitionTo(OrderStatus.Paid, CreatedAt.AddDays(1));
        }

        if (targetStatus >= OrderStatus.InDevelopment && targetStatus != OrderStatus.Cancelled)
        {
            order.TransitionTo(OrderStatus.InDevelopment, CreatedAt.AddDays(2));
        }

        if (targetStatus == OrderStatus.Completed)
        {
            order.TransitionTo(OrderStatus.Completed, CreatedAt.AddDays(5));
        }

        return order;
    }
}
