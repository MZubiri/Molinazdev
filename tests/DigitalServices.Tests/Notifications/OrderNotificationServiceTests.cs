using DigitalServices.Application.Notifications;
using DigitalServices.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DigitalServices.Tests.Notifications;

public sealed class OrderNotificationServiceTests
{
    [Fact]
    public void NotificationOptions_DefaultValues_AreDisabledAndSafe()
    {
        var options = new NotificationOptions();

        Assert.False(options.Email.Enabled);
        Assert.False(options.AdminAlert.Enabled);
        Assert.Null(options.AdminAlert.DiscordWebhookUrl);
        Assert.Null(options.AdminAlert.TelegramBotToken);
    }

    [Fact]
    public async Task NotifyOrderPaidAsync_WhenDisabled_CompletesWithoutErrors()
    {
        var options = Options.Create(new NotificationOptions
        {
            Email = new EmailOptions { Enabled = false },
            AdminAlert = new AdminAlertOptions { Enabled = false }
        });

        using var httpClient = new HttpClient();
        var service = new OrderNotificationService(
            options,
            httpClient,
            NullLogger<OrderNotificationService>.Instance);

        var notification = new OrderPaidNotification(
            Guid.NewGuid(),
            "ORD-1001",
            Guid.NewGuid(),
            "Landing Page",
            885.00m,
            "PEN",
            "Carlos Mendoza",
            "carlos@empresa.pe",
            "+51999888777",
            "Mendoza Tech",
            DateTimeOffset.UtcNow);

        // Act & Assert - should not throw
        await service.NotifyOrderPaidAsync(notification, CancellationToken.None);
    }

    [Fact]
    public async Task NotifyOrderPaidAsync_WhenEnabledWithEmptyCredentials_DoesNotThrow()
    {
        var options = Options.Create(new NotificationOptions
        {
            Email = new EmailOptions { Enabled = false },
            AdminAlert = new AdminAlertOptions
            {
                Enabled = true,
                DiscordWebhookUrl = "",
                TelegramBotToken = "",
                TelegramChatId = ""
            }
        });

        using var httpClient = new HttpClient();
        var service = new OrderNotificationService(
            options,
            httpClient,
            NullLogger<OrderNotificationService>.Instance);

        var notification = new OrderPaidNotification(
            Guid.NewGuid(),
            "ORD-1002",
            Guid.NewGuid(),
            "Ecommerce Full",
            2360.00m,
            "PEN",
            "Lucia Perez",
            "lucia@perez.com",
            null,
            null,
            DateTimeOffset.UtcNow);

        // Act & Assert - should gracefully skip webhooks without exceptions
        await service.NotifyOrderPaidAsync(notification, CancellationToken.None);
    }
}
