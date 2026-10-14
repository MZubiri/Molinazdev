namespace DigitalServices.Application.Notifications;

public sealed record OrderPaidNotification(
    Guid OrderId,
    string OrderNumber,
    Guid PackageId,
    string PackageName,
    decimal TotalAmount,
    string Currency,
    string ClientName,
    string ClientEmail,
    string? ClientPhone,
    string? CompanyName,
    DateTimeOffset PaidAt);
