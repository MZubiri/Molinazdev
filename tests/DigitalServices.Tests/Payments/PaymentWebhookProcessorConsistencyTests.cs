using DigitalServices.Application.Payments;
using DigitalServices.Domain.Entities;
using DigitalServices.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace DigitalServices.Tests.Payments;

public sealed class PaymentWebhookProcessorConsistencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_AmountMismatch_DoesNotPersistOrChangeOrder()
    {
        var fixture = CreateFixture();
        fixture.Gateway.GetPaymentHandler = id => fixture.ValidPayment with
        {
            GatewayPaymentId = id,
            Amount = fixture.Order.TotalAmount + 0.01m
        };

        var result = await fixture.Processor.ProcessAsync(Command(), CancellationToken.None);

        Assert.Equal(PaymentWebhookOutcome.InconsistentPayment, result.Outcome);
        Assert.Equal(OrderStatus.Pending, fixture.Order.Status);
        Assert.Equal(0, fixture.Store.ApplyCalls);
        Assert.Equal(0, fixture.Store.PaymentCount);
    }

    [Fact]
    public async Task ProcessAsync_CurrencyMismatch_DoesNotPersistOrChangeOrder()
    {
        var fixture = CreateFixture();
        fixture.Gateway.GetPaymentHandler = id => fixture.ValidPayment with
        {
            GatewayPaymentId = id,
            Currency = "USD"
        };

        var result = await fixture.Processor.ProcessAsync(Command(), CancellationToken.None);

        Assert.Equal(PaymentWebhookOutcome.InconsistentPayment, result.Outcome);
        Assert.Equal(OrderStatus.Pending, fixture.Order.Status);
        Assert.Equal(0, fixture.Store.ApplyCalls);
    }

    [Fact]
    public async Task ProcessAsync_MalformedExternalReference_DoesNotPersist()
    {
        var fixture = CreateFixture();
        fixture.Gateway.GetPaymentHandler = id => fixture.ValidPayment with
        {
            GatewayPaymentId = id,
            ExternalReference = "order:not-a-guid"
        };

        var result = await fixture.Processor.ProcessAsync(Command(), CancellationToken.None);

        Assert.Equal(PaymentWebhookOutcome.InconsistentPayment, result.Outcome);
        Assert.Equal(0, fixture.Store.ApplyCalls);
        Assert.Equal(0, fixture.Store.PaymentCount);
    }

    [Fact]
    public async Task ProcessAsync_UnknownWellFormedExternalReference_DoesNotPersist()
    {
        var fixture = CreateFixture();
        fixture.Gateway.GetPaymentHandler = id => fixture.ValidPayment with
        {
            GatewayPaymentId = id,
            ExternalReference = Order.BuildExternalReference(Guid.NewGuid())
        };

        var result = await fixture.Processor.ProcessAsync(Command(), CancellationToken.None);

        Assert.Equal(PaymentWebhookOutcome.OrderNotFound, result.Outcome);
        Assert.Equal(0, fixture.Store.ApplyCalls);
    }

    [Fact]
    public async Task ProcessAsync_ProviderPaymentIdMismatch_DoesNotPersist()
    {
        var fixture = CreateFixture();
        fixture.Gateway.GetPaymentHandler = _ => fixture.ValidPayment with
        {
            GatewayPaymentId = "a-different-payment"
        };

        var result = await fixture.Processor.ProcessAsync(Command(), CancellationToken.None);

        Assert.Equal(PaymentWebhookOutcome.InconsistentPayment, result.Outcome);
        Assert.Equal(0, fixture.Store.ApplyCalls);
    }

    [Fact]
    public async Task ProcessAsync_ValidVerifiedPayment_PersistsAndMarksPendingOrderPaid()
    {
        var fixture = CreateFixture();
        fixture.Gateway.GetPaymentHandler = id => fixture.ValidPayment with { GatewayPaymentId = id };

        var result = await fixture.Processor.ProcessAsync(Command(), CancellationToken.None);

        Assert.Equal(PaymentWebhookOutcome.Processed, result.Outcome);
        Assert.Equal(fixture.Order.Id, result.OrderId);
        Assert.NotNull(result.PaymentId);
        Assert.Equal(1, fixture.Store.ApplyCalls);
        Assert.Equal(1, fixture.Store.PaymentCount);
        Assert.Equal(OrderStatus.Paid, fixture.Order.Status);
        Assert.Equal(Now, fixture.Order.PaidAt);
    }

    private static ProcessPaymentWebhookCommand Command(
        string notificationId = "notification-001")
    {
        return new ProcessPaymentWebhookCommand(
            "payment",
            "123456789",
            notificationId,
            "{\"type\":\"payment\",\"data\":{\"id\":\"123456789\"}}",
            "ts=valid,v1=valid",
            "request-001");
    }

    private static ProcessorFixture CreateFixture()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 750.25m, "PEN", Now.AddHours(-1));
        var orders = new InMemoryOrderRepository();
        orders.Seed(order);
        var payments = new InMemoryPaymentRepository();
        var store = new ThreadSafePaymentWebhookStore(order);
        var gateway = new TestPaymentGateway();
        var validPayment = new GatewayPaymentDetails(
            "123456789",
            order.ExternalReference,
            order.TotalAmount,
            order.Currency,
            "approved",
            "accredited",
            "preference-001");

        var processor = new PaymentWebhookProcessor(
            new ConstantSignatureValidator(isValid: true),
            gateway,
            orders,
            payments,
            store,
            NullLogger<PaymentWebhookProcessor>.Instance,
            new FixedTimeProvider(Now));

        return new ProcessorFixture(order, gateway, store, validPayment, processor);
    }

    private sealed record ProcessorFixture(
        Order Order,
        TestPaymentGateway Gateway,
        ThreadSafePaymentWebhookStore Store,
        GatewayPaymentDetails ValidPayment,
        PaymentWebhookProcessor Processor);
}
