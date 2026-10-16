using DigitalServices.Application.Chatbot;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DigitalServices.Web.Controllers.Api;

[ApiController]
[Route("api/chat")]
[EnableRateLimiting("ChatPolicy")]
public sealed class ChatApiController(IChatbotService chatbotService) : ControllerBase
{
    public sealed record SendMessageRequest(string? Message, List<ChatMessageDto>? History);

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Message))
        {
            return BadRequest(new { message = "El mensaje no puede estar vacío." });
        }

        var trimmedMessage = request.Message.Trim();
        if (trimmedMessage.Length > 1000)
        {
            return BadRequest(new { message = "El mensaje no puede exceder los 1000 caracteres." });
        }

        var result = await chatbotService.GenerateReplyAsync(
            new ChatRequestDto(trimmedMessage, request.History),
            cancellationToken);

        return Ok(new
        {
            reply = result.Reply,
            model = result.ModelUsed,
            isFallback = result.IsFallback
        });
    }
}
