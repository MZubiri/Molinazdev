using System.Text.RegularExpressions;
using DigitalServices.Domain.Entities;

namespace DigitalServices.Tests.Domain;

public sealed class ExternalReferenceTests
{
    [Fact]
    public void Create_GeneratesReadableUniqueOrderNumberAndUnambiguousExternalReference()
    {
        var createdAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", createdAt);

        Assert.Matches(new Regex("^ORD-2026-[0-9A-F]{12}$", RegexOptions.CultureInvariant), order.OrderNumber);
        Assert.Equal($"order:{order.Id:N}", order.ExternalReference);
        Assert.True(Order.TryParseExternalReference(order.ExternalReference, out var parsedOrderId));
        Assert.Equal(order.Id, parsedOrderId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ORDER:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("order:not-a-guid")]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    public void TryParseExternalReference_InvalidValue_IsRejected(string? externalReference)
    {
        Assert.False(Order.TryParseExternalReference(externalReference, out var orderId));
        Assert.Equal(Guid.Empty, orderId);
    }

    [Fact]
    public void BuildExternalReference_UsesAllGuidBitsRatherThanReadableNumberSuffix()
    {
        var first = Guid.ParseExact("aaaaaaaaaaaa00000000000000000001", "N");
        var second = Guid.ParseExact("aaaaaaaaaaaa00000000000000000002", "N");

        var firstReference = Order.BuildExternalReference(first);
        var secondReference = Order.BuildExternalReference(second);

        Assert.NotEqual(firstReference, secondReference);
        Assert.EndsWith(first.ToString("N"), firstReference, StringComparison.Ordinal);
        Assert.EndsWith(second.ToString("N"), secondReference, StringComparison.Ordinal);
    }
}
