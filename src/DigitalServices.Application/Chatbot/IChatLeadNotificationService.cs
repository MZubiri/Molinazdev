namespace DigitalServices.Application.Chatbot;

public sealed record ChatLeadNotificationDto(
    string Contact,
    string? PhoneNumber,
    string? Email,
    string? WhatsAppUrl,
    string LastUserMessage,
    IReadOnlyList<ChatMessageDto>? History,
    string? ClientIp = null
);

public interface IChatLeadNotificationService
{
    Task NotifyChatLeadAsync(ChatLeadNotificationDto lead, CancellationToken cancellationToken = default);
}
