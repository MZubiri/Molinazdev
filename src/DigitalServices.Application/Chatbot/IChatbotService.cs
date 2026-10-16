namespace DigitalServices.Application.Chatbot;

public sealed record ChatMessageDto(string Role, string Content);

public sealed record ChatRequestDto(string Message, IReadOnlyList<ChatMessageDto>? History = null);

public sealed record ChatResponseDto(string Reply, string ModelUsed, bool IsFallback);

public interface IChatbotService
{
    Task<ChatResponseDto> GenerateReplyAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}
