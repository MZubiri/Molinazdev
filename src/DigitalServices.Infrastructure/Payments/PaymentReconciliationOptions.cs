namespace DigitalServices.Infrastructure.Payments;

public sealed class PaymentReconciliationOptions
{
    public const string SectionName = "PaymentReconciliation";

    public bool Enabled { get; init; } = true;

    public int IntervalMinutes { get; init; } = 15;

    public int PendingThresholdMinutes { get; init; } = 10;
}
