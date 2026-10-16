namespace DigitalServices.Infrastructure.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public EmailOptions Email { get; init; } = new();

    public AdminAlertOptions AdminAlert { get; init; } = new();
}

public sealed class EmailOptions
{
    public bool Enabled { get; init; }

    public string SmtpHost { get; init; } = string.Empty;

    public int SmtpPort { get; init; } = 587;

    public string SmtpUsername { get; init; } = string.Empty;

    public string SmtpPassword { get; init; } = string.Empty;

    public string FromAddress { get; init; } = "noreply@molinazdev.lat";

    public string FromName { get; init; } = "MolinazDev";

    public bool EnableSsl { get; init; } = true;
}

public sealed class AdminAlertOptions
{
    public bool Enabled { get; init; }

    public string? DiscordWebhookUrl { get; init; }

    public string? TelegramBotToken { get; init; }

    public string? TelegramChatId { get; init; }

    public string AdminEmail { get; init; } = "Molinazdepe@gmail.com";
}
