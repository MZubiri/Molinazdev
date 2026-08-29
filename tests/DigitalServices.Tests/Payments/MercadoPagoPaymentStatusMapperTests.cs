using DigitalServices.Application.Payments;
using DigitalServices.Domain.Enums;

namespace DigitalServices.Tests.Payments;

public sealed class MercadoPagoPaymentStatusMapperTests
{
    [Theory]
    [InlineData("approved", GatewayPaymentSemantic.Approved, OrderStatus.Paid, true)]
    [InlineData("pending", GatewayPaymentSemantic.AwaitingPayment, OrderStatus.Pending, false)]
    [InlineData("in_process", GatewayPaymentSemantic.AwaitingPayment, OrderStatus.Pending, false)]
    [InlineData("authorized", GatewayPaymentSemantic.AwaitingPayment, OrderStatus.Pending, false)]
    [InlineData("in_mediation", GatewayPaymentSemantic.AwaitingPayment, OrderStatus.Pending, false)]
    [InlineData("rejected", GatewayPaymentSemantic.Rejected, OrderStatus.Pending, true)]
    [InlineData("cancelled", GatewayPaymentSemantic.Cancelled, OrderStatus.Cancelled, true)]
    public void Map_KnownStatus_ReturnsExplicitOrderMapping(
        string gatewayStatus,
        GatewayPaymentSemantic semantic,
        OrderStatus expectedOrderStatus,
        bool isTerminal)
    {
        var result = MercadoPagoPaymentStatusMapper.Map(gatewayStatus);

        Assert.True(result.IsKnown);
        Assert.Equal(semantic, result.Semantic);
        Assert.Equal(expectedOrderStatus, result.TargetOrderStatus);
        Assert.Equal(isTerminal, result.IsTerminal);
    }

    [Theory]
    [InlineData("refunded", GatewayPaymentSemantic.Refunded, true)]
    [InlineData("charged_back", GatewayPaymentSemantic.ChargedBack, true)]
    [InlineData("partially_refunded", GatewayPaymentSemantic.PartiallyRefunded, false)]
    public void Map_RefundOrChargeback_IsExplicitButRequiresManualOrderHandling(
        string gatewayStatus,
        GatewayPaymentSemantic semantic,
        bool isTerminal)
    {
        var result = MercadoPagoPaymentStatusMapper.Map(gatewayStatus);

        Assert.True(result.IsKnown);
        Assert.Equal(semantic, result.Semantic);
        Assert.Null(result.TargetOrderStatus);
        Assert.Equal(isTerminal, result.IsTerminal);
    }

    [Fact]
    public void Map_IsCaseAndWhitespaceInsensitive()
    {
        var result = MercadoPagoPaymentStatusMapper.Map("  APPROVED  ");

        Assert.Equal("approved", result.NormalizedStatus);
        Assert.Equal(OrderStatus.Paid, result.TargetOrderStatus);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("future_status")]
    public void Map_UnknownStatus_DoesNotMutateOrder(string? gatewayStatus)
    {
        var result = MercadoPagoPaymentStatusMapper.Map(gatewayStatus);

        Assert.False(result.IsKnown);
        Assert.Equal(GatewayPaymentSemantic.Unknown, result.Semantic);
        Assert.Null(result.TargetOrderStatus);
        Assert.False(result.IsTerminal);
    }
}
