using DigitalServices.Application.Checkout;
using DigitalServices.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace DigitalServices.Tests.Checkout;

public sealed class CheckoutPriceSecurityTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 15, 30, 0, TimeSpan.Zero);

    [Fact]
    public void CreateCheckoutCommand_DoesNotExposeAmountPriceOrCurrencyForModelBinding()
    {
        var propertyNames = typeof(CreateCheckoutCommand)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Amount", propertyNames);
        Assert.DoesNotContain("Price", propertyNames);
        Assert.DoesNotContain("TotalAmount", propertyNames);
        Assert.DoesNotContain("Currency", propertyNames);
        Assert.Contains("PackageId", propertyNames);
    }

    [Fact]
    public async Task CreateAsync_AddsIgvToRepositoryPriceForOrderPreferenceAndPayment()
    {
        var package = CreatePackage(9_876.54m, "pen");
        var catalog = new TestCatalogRepository { ActivePackage = package };
        var clients = new InMemoryClientRepository();
        var orders = new InMemoryOrderRepository();
        var payments = new InMemoryPaymentRepository();
        var unitOfWork = new CountingUnitOfWork();
        var gateway = new TestPaymentGateway();
        var service = new CheckoutService(
            catalog,
            clients,
            orders,
            payments,
            unitOfWork,
            gateway,
            NullLogger<CheckoutService>.Instance,
            new FixedTimeProvider(Now));

        var result = await service.CreateAsync(
            new CreateCheckoutCommand
            {
                PackageId = package.Id,
                FullName = "Ada Lovelace",
                Email = "BUYER@Example.COM",
                CompanyName = "Analytical Engines"
            },
            CancellationToken.None);

        var order = Assert.Single(orders.Orders);
        var payment = Assert.Single(payments.Payments);
        var preferenceRequest = Assert.IsType<DigitalServices.Application.Payments.CreatePaymentPreferenceRequest>(
            gateway.LastPreferenceRequest);
        var expectedTotal = decimal.Round(package.Price * 1.18m, 2, MidpointRounding.AwayFromZero);

        Assert.Equal(expectedTotal, order.TotalAmount);
        Assert.Equal(package.Currency, order.Currency);
        Assert.Equal(expectedTotal, preferenceRequest.Amount);
        Assert.Equal(package.Currency, preferenceRequest.Currency);
        Assert.Equal(order.ExternalReference, preferenceRequest.ExternalReference);
        Assert.Equal(expectedTotal, payment.Amount);
        Assert.Equal(package.Currency, payment.Currency);
        Assert.Equal(order.Id, payment.OrderId);
        Assert.Equal("pref-test-001", payment.PreferenceId);
        Assert.Null(payment.GatewayPaymentId);
        Assert.Equal("buyer@example.com", Assert.Single(clients.Clients).Email);
        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(2, unitOfWork.SaveCount);
        Assert.Equal(1, gateway.CreatePreferenceCalls);
    }

    [Fact]
    public async Task CreateAsync_UnknownOrInactivePackage_DoesNotCallGatewayOrPersistOrder()
    {
        var orders = new InMemoryOrderRepository();
        var gateway = new TestPaymentGateway();
        var service = new CheckoutService(
            new TestCatalogRepository(),
            new InMemoryClientRepository(),
            orders,
            new InMemoryPaymentRepository(),
            new CountingUnitOfWork(),
            gateway,
            NullLogger<CheckoutService>.Instance,
            new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<DigitalServices.Application.Common.ResourceNotFoundException>(
            () => service.CreateAsync(
                new CreateCheckoutCommand
                {
                    PackageId = Guid.NewGuid(),
                    FullName = "Grace Hopper",
                    Email = "grace@example.com"
                },
                CancellationToken.None));

        Assert.Empty(orders.Orders);
        Assert.Equal(0, gateway.CreatePreferenceCalls);
    }

    private static ServicePackage CreatePackage(decimal price, string currency)
    {
        return ServicePackage.Create(
            Guid.NewGuid(),
            "Enterprise Platform",
            "Custom software delivery",
            price,
            currency,
            45,
            ["Discovery", "Implementation", "Deployment"],
            Now);
    }
}
