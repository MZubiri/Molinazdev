using DigitalServices.Application.Notifications;
using DigitalServices.Application.Payments;
using DigitalServices.Domain.Entities;
using DigitalServices.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace DigitalServices.Tests.Notifications;

public sealed class PaymentNotificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_WhenPaymentApproved_DispatchesNotificationWithCorrectData()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new ProcessPaymentWebhookCommand(
            "payment",
            "123456789",
            "notif-001",
            "{\"action\":\"payment.created\"}",
            "ts=valid,v1=valid",
            "request-001");

        // Act
        var result = await fixture.Processor.ProcessAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(PaymentWebhookOutcome.Processed, result.Outcome);
        Assert.Single(fixture.NotificationService.Notifications);

        var notification = fixture.NotificationService.Notifications[0];
        Assert.Equal(fixture.Order.OrderNumber, notification.OrderNumber);
        Assert.Equal(fixture.Client.Email, notification.ClientEmail);
        Assert.Equal(fixture.Client.FullName, notification.ClientName);
        Assert.Equal(fixture.Package.Name, notification.PackageName);
        Assert.Equal(fixture.Order.TotalAmount, notification.TotalAmount);
        Assert.Equal("PEN", notification.Currency);
    }

    [Fact]
    public async Task ProcessAsync_WhenNotificationFails_PaymentStillSuccessfullyApplied()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.NotificationService.ShouldThrow = true;

        var command = new ProcessPaymentWebhookCommand(
            "payment",
            "123456789",
            "notif-001",
            "{\"action\":\"payment.created\"}",
            "ts=valid,v1=valid",
            "request-001");

        // Act
        var result = await fixture.Processor.ProcessAsync(command, CancellationToken.None);

        // Assert - payment is processed despite notification error
        Assert.Equal(PaymentWebhookOutcome.Processed, result.Outcome);
        Assert.Equal(OrderStatus.Paid, fixture.Order.Status);
    }

    [Fact]
    public async Task ProcessAsync_WhenPaymentPending_NotificationNotDispatched()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.Gateway.GetPaymentHandler = id => fixture.ValidPayment with
        {
            GatewayPaymentId = id,
            Status = "pending",
            StatusDetail = "pending_waiting_transfer"
        };

        var command = new ProcessPaymentWebhookCommand(
            "payment",
            "123456789",
            "notif-001",
            "{\"action\":\"payment.created\"}",
            "ts=valid,v1=valid",
            "request-001");

        // Act
        var result = await fixture.Processor.ProcessAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(PaymentWebhookOutcome.Processed, result.Outcome);
        Assert.Empty(fixture.NotificationService.Notifications);
    }

    [Fact]
    public async Task ProcessAsync_WhenPaymentRejected_NotificationNotDispatched()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.Gateway.GetPaymentHandler = id => fixture.ValidPayment with
        {
            GatewayPaymentId = id,
            Status = "rejected",
            StatusDetail = "cc_rejected_insufficient_amount"
        };

        var command = new ProcessPaymentWebhookCommand(
            "payment",
            "123456789",
            "notif-001",
            "{\"action\":\"payment.created\"}",
            "ts=valid,v1=valid",
            "request-001");

        // Act
        var result = await fixture.Processor.ProcessAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(PaymentWebhookOutcome.Processed, result.Outcome);
        Assert.Empty(fixture.NotificationService.Notifications);
    }

    private static Fixture CreateFixture()
    {
        var client = Client.Create("Carlos Mendoza", "carlos@empresa.pe", "+51999888777", "Mendoza Tech", Now);
        var package = ServicePackage.Create(
            Guid.NewGuid(),
            "Web Corporativa Pro",
            "Sitio web corporativo de alto impacto",
            1500m,
            "PEN",
            14,
            new[] { "Diseño Responsive", "SEO" },
            Now,
            isActive: true);

        var order = Order.Create(client.Id, package.Id, 1770m, "PEN", Now.AddMinutes(-30));

        var clients = new InMemoryClientRepository();
        clients.Seed(client);

        var catalog = new TestCatalogRepository
        {
            ActivePackage = package
        };

        var orders = new InMemoryOrderRepository();
        orders.Seed(order);

        var payments = new InMemoryPaymentRepository();
        var store = new ThreadSafePaymentWebhookStore(order);
        var gateway = new TestPaymentGateway();
        var notificationService = new TestOrderNotificationService();

        var validPayment = new GatewayPaymentDetails(
            "123456789",
            order.ExternalReference,
            order.TotalAmount,
            order.Currency,
            "approved",
            "accredited",
            "preference-001");

        gateway.GetPaymentHandler = _ => validPayment;

        var processor = new PaymentWebhookProcessor(
            new ConstantSignatureValidator(isValid: true),
            gateway,
            orders,
            payments,
            store,
            NullLogger<PaymentWebhookProcessor>.Instance,
            new FixedTimeProvider(Now),
            notificationService,
            clients,
            catalog);

        return new Fixture(order, client, package, gateway, notificationService, processor, validPayment);
    }

    private sealed record Fixture(
        Order Order,
        Client Client,
        ServicePackage Package,
        TestPaymentGateway Gateway,
        TestOrderNotificationService NotificationService,
        PaymentWebhookProcessor Processor,
        GatewayPaymentDetails ValidPayment);
}
