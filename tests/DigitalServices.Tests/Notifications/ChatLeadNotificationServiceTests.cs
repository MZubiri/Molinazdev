using DigitalServices.Application.Chatbot;
using DigitalServices.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalServices.Tests.Notifications;

public sealed class ChatLeadNotificationServiceTests
{
    [Fact]
    public async Task NotifyChatLeadAsync_WhenDisabled_CompletesWithoutErrors()
    {
        var options = Options.Create(new NotificationOptions
        {
            Email = new EmailOptions { Enabled = false },
            AdminAlert = new AdminAlertOptions { Enabled = false, AdminEmail = "contacto@molinazdev.lat" }
        });

        using var httpClient = new HttpClient();
        var service = new ChatLeadNotificationService(
            options,
            httpClient,
            NullLogger<ChatLeadNotificationService>.Instance);

        var lead = new ChatLeadNotificationDto(
            Contact: "+51 934 772 764",
            PhoneNumber: "934772764",
            Email: null,
            WhatsAppUrl: "https://wa.me/51934772764",
            LastUserMessage: "934772764",
            History: [new ChatMessageDto("user", "Hola, quiero un chatbot")],
            ClientIp: "127.0.0.1"
        );

        // Act & Assert - should execute gracefully without throwing
        await service.NotifyChatLeadAsync(lead, CancellationToken.None);
    }

    [Fact]
    public async Task NotifyChatLeadAsync_WhenLeadIsNull_ThrowsArgumentNullException()
    {
        var options = Options.Create(new NotificationOptions());
        using var httpClient = new HttpClient();
        var service = new ChatLeadNotificationService(
            options,
            httpClient,
            NullLogger<ChatLeadNotificationService>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.NotifyChatLeadAsync(null!, CancellationToken.None));
    }
}
