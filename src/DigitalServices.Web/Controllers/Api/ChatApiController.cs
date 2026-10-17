using DigitalServices.Application.Chatbot;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DigitalServices.Web.Controllers.Api;

[ApiController]
[Route("api/chat")]
[EnableRateLimiting("ChatPolicy")]
public sealed class ChatApiController(
    IChatbotService chatbotService,
    IChatLeadNotificationService leadNotificationService,
    ILogger<ChatApiController> logger) : ControllerBase
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

        // Detect if user provided contact details (phone number / email)
        var contact = ChatContactExtractor.ExtractContact(trimmedMessage);
        if (contact is not null)
        {
            try
            {
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var leadDto = new ChatLeadNotificationDto(
                    Contact: contact.FormattedContact,
                    PhoneNumber: contact.PhoneNumber,
                    Email: contact.Email,
                    WhatsAppUrl: contact.WhatsAppUrl,
                    LastUserMessage: trimmedMessage,
                    History: request.History,
                    ClientIp: clientIp
                );

                await leadNotificationService.NotifyChatLeadAsync(leadDto, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch chat lead notification for contact: {Contact}", contact.FormattedContact);
            }
        }

        var result = await chatbotService.GenerateReplyAsync(
            new ChatRequestDto(trimmedMessage, request.History),
            cancellationToken);

        return Ok(new
        {
            reply = result.Reply,
            model = result.ModelUsed,
            isFallback = result.IsFallback,
            leadCaptured = contact is not null
        });
    }
}
