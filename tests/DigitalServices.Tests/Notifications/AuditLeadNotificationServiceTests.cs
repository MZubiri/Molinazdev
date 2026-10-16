using DigitalServices.Application.Leads;
using DigitalServices.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalServices.Tests.Notifications;

public sealed class AuditLeadNotificationServiceTests
{
    [Fact]
    public async Task NotifyAuditLeadAsync_WhenDisabled_CompletesWithoutErrors()
    {
        var options = Options.Create(new NotificationOptions
        {
            Email = new EmailOptions { Enabled = false },
            AdminAlert = new AdminAlertOptions { Enabled = false, AdminEmail = "contacto@molinazdev.lat" }
        });

        using var httpClient = new HttpClient();
        var service = new AuditLeadNotificationService(
            options,
            httpClient,
            NullLogger<AuditLeadNotificationService>.Instance);

        var lead = new AuditLeadRequestDto("https://ejemplo.com", "cliente@empresa.com");

        // Act & Assert - should execute gracefully
        await service.NotifyAuditLeadAsync(lead, "127.0.0.1", CancellationToken.None);
    }

    [Fact]
    public async Task NotifyAuditLeadAsync_WhenLeadIsNull_ThrowsArgumentNullException()
    {
        var options = Options.Create(new NotificationOptions());
        using var httpClient = new HttpClient();
        var service = new AuditLeadNotificationService(
            options,
            httpClient,
            NullLogger<AuditLeadNotificationService>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.NotifyAuditLeadAsync(null!, "127.0.0.1", CancellationToken.None));
    }
}
