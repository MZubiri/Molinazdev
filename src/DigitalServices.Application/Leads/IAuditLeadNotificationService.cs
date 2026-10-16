namespace DigitalServices.Application.Leads;

public interface IAuditLeadNotificationService
{
    Task NotifyAuditLeadAsync(
        AuditLeadRequestDto lead,
        string? clientIp = null,
        CancellationToken cancellationToken = default);
}
