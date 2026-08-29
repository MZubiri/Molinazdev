using System.Collections.Concurrent;
using DigitalServices.Application.Payments;
using DigitalServices.Domain.Entities;
using DigitalServices.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace DigitalServices.Tests.Payments;

public sealed class PaymentWebhookIdempotencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 19, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_SameNotificationTwice_CreatesOnlyOnePayment()
    {
        var fixture = CreateFixture("approved");
        var command = Command("notification-repeat");

        var first = await fixture.Processor.ProcessAsync(command, CancellationToken.None);
        var second = await fixture.Processor.ProcessAsync(command, CancellationToken.None);

        Assert.Equal(PaymentWebhookOutcome.Processed, first.Outcome);
        Assert.Equal(PaymentWebhookOutcome.Duplicate, second.Outcome);
        Assert.Equal(1, fixture.Store.PaymentCount);
        Assert.Equal(1, fixture.Store.ApplyCalls);
        Assert.Equal(1, fixture.Gateway.GetPaymentCalls);
        Assert.Equal(OrderStatus.Paid, fixture.Order.Status);
    }

    [Fact]
    public async Task ProcessAsync_ConcurrentSameNotification_IsAtomicAndCreatesOnePayment()
    {
        var fixture = CreateFixture("approved", TimeSpan.FromMilliseconds(25));
        var command = Command("notification-concurrent");

        var tasks = Enumerable.Range(0, 12)
            .Select(_ => fixture.Processor.ProcessAsync(command, CancellationToken.None));
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, results.Count(result => result.Outcome == PaymentWebhookOutcome.Processed));
        Assert.Equal(11, results.Count(result => result.Outcome == PaymentWebhookOutcome.Duplicate));
        Assert.Equal(1, fixture.Store.PaymentCount);
        Assert.Equal(OrderStatus.Paid, fixture.Order.Status);
    }

    [Fact]
    public async Task ProcessAsync_SamePaymentWithNewApprovedStatus_UpdatesExistingPaymentAndOrder()
    {
        var order = CreateOrder();
        var responses = new ConcurrentQueue<GatewayPaymentDetails>(
        [
            Payment(order, "pending", "pending_waiting_payment"),
            Payment(order, "approved", "accredited")
        ]);
        var gateway = new TestPaymentGateway
        {
            GetPaymentHandler = _ => responses.TryDequeue(out var response) ? response : null
        };
        var fixture = CreateFixture(order, gateway);

        var pendingResult = await fixture.Processor.ProcessAsync(
            Command("notification-pending"),
            CancellationToken.None);
        var approvedResult = await fixture.Processor.ProcessAsync(
            Command("notification-approved"),
            CancellationToken.None);

        Assert.Equal(PaymentWebhookOutcome.Processed, pendingResult.Outcome);
        Assert.Equal(PaymentWebhookOutcome.Processed, approvedResult.Outcome);
        Assert.Equal(1, fixture.Store.PaymentCount);
        Assert.Equal("approved", fixture.Store.GetStoredStatus("MercadoPago", "payment-001"));
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public async Task ProcessAsync_SamePaymentAndStateWithDifferentNotification_DoesNotDuplicateRow()
    {
        var fixture = CreateFixture("approved");

        var first = await fixture.Processor.ProcessAsync(
            Command("notification-a"),
            CancellationToken.None);
        var second = await fixture.Processor.ProcessAsync(
            Command("notification-b"),
            CancellationToken.None);

        Assert.Equal(PaymentWebhookOutcome.Processed, first.Outcome);
        Assert.Equal(PaymentWebhookOutcome.Duplicate, second.Outcome);
        Assert.Equal(1, fixture.Store.PaymentCount);
    }

    private static IdempotencyFixture CreateFixture(
        string status,
        TimeSpan delay = default)
    {
        var order = CreateOrder();
        var gateway = new TestPaymentGateway
        {
            GetPaymentDelay = delay,
            GetPaymentHandler = _ => Payment(order, status, "test-detail")
        };

        return CreateFixture(order, gateway);
    }

    private static IdempotencyFixture CreateFixture(Order order, TestPaymentGateway gateway)
    {
        var orders = new InMemoryOrderRepository();
        orders.Seed(order);
        var store = new ThreadSafePaymentWebhookStore(order);
        var processor = new PaymentWebhookProcessor(
            new ConstantSignatureValidator(isValid: true),
            gateway,
            orders,
            new InMemoryPaymentRepository(),
            store,
            NullLogger<PaymentWebhookProcessor>.Instance,
            new FixedTimeProvider(Now));

        return new IdempotencyFixture(order, gateway, store, processor);
    }

    private static Order CreateOrder()
    {
        return Order.Create(Guid.NewGuid(), Guid.NewGuid(), 1_250m, "PEN", Now.AddMinutes(-10));
    }

    private static GatewayPaymentDetails Payment(Order order, string status, string statusDetail)
    {
        return new GatewayPaymentDetails(
            "payment-001",
            order.ExternalReference,
            order.TotalAmount,
            order.Currency,
            status,
            statusDetail,
            "preference-001");
    }

    private static ProcessPaymentWebhookCommand Command(string notificationId)
    {
        return new ProcessPaymentWebhookCommand(
            "payment",
            "payment-001",
            notificationId,
            "{\"type\":\"payment\"}",
            "ts=valid,v1=valid",
            "request-idempotency");
    }

    private sealed record IdempotencyFixture(
        Order Order,
        TestPaymentGateway Gateway,
        ThreadSafePaymentWebhookStore Store,
        PaymentWebhookProcessor Processor);
}
