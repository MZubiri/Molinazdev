using DigitalServices.Domain.Enums;

namespace DigitalServices.Application.Payments;

public enum GatewayPaymentSemantic
{
    Unknown = 0,
    AwaitingPayment = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    Refunded = 5,
    ChargedBack = 6,
    PartiallyRefunded = 7
}

public sealed record PaymentStatusMapping(
    string NormalizedStatus,
    GatewayPaymentSemantic Semantic,
    OrderStatus? TargetOrderStatus,
    bool IsKnown,
    bool IsTerminal);

public static class MercadoPagoPaymentStatusMapper
{
    public static PaymentStatusMapping Map(string? gatewayStatus)
    {
        var status = gatewayStatus?.Trim().ToLowerInvariant() ?? string.Empty;

        return status switch
        {
            "approved" => Known(status, GatewayPaymentSemantic.Approved, OrderStatus.Paid, isTerminal: true),
            "pending" or "in_process" or "authorized" or "in_mediation" =>
                Known(status, GatewayPaymentSemantic.AwaitingPayment, OrderStatus.Pending, isTerminal: false),
            "rejected" => Known(status, GatewayPaymentSemantic.Rejected, OrderStatus.Pending, isTerminal: true),
            "cancelled" => Known(status, GatewayPaymentSemantic.Cancelled, OrderStatus.Cancelled, isTerminal: true),
            // Refunds and chargebacks are persisted and surfaced for manual review. They must not
            // erase delivery progress or automatically reinterpret a completed business order.
            "refunded" => Known(status, GatewayPaymentSemantic.Refunded, targetOrderStatus: null, isTerminal: true),
            "charged_back" => Known(
                status,
                GatewayPaymentSemantic.ChargedBack,
                targetOrderStatus: null,
                isTerminal: true),
            // A partial refund requires human/business-policy handling and must not cancel the order automatically.
            "partially_refunded" => Known(
                status,
                GatewayPaymentSemantic.PartiallyRefunded,
                targetOrderStatus: null,
                isTerminal: false),
            _ => new PaymentStatusMapping(
                status,
                GatewayPaymentSemantic.Unknown,
                TargetOrderStatus: null,
                IsKnown: false,
                IsTerminal: false)
        };
    }

    public static OrderStatus? MapToOrderStatus(string? gatewayStatus)
    {
        return Map(gatewayStatus).TargetOrderStatus;
    }

    private static PaymentStatusMapping Known(
        string normalizedStatus,
        GatewayPaymentSemantic semantic,
        OrderStatus? targetOrderStatus,
        bool isTerminal)
    {
        return new PaymentStatusMapping(
            normalizedStatus,
            semantic,
            targetOrderStatus,
            IsKnown: true,
            isTerminal);
    }
}
