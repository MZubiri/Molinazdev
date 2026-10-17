using DigitalServices.Application.Chatbot;
using DigitalServices.Web.Controllers.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DigitalServices.Tests.Chatbot;

public sealed class ChatApiControllerTests
{
    private sealed class FakeChatbotService(string reply) : IChatbotService
    {
        public Task<ChatResponseDto> GenerateReplyAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ChatResponseDto(reply, "test-model", false));
        }
    }

    private sealed class FakeChatLeadNotificationService : IChatLeadNotificationService
    {
        public ChatLeadNotificationDto? LastNotifiedLead { get; private set; }

        public Task NotifyChatLeadAsync(ChatLeadNotificationDto lead, CancellationToken cancellationToken = default)
        {
            LastNotifiedLead = lead;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SendMessage_WhenMessageContainsPhone_NotifiesLeadAndReturnsReply()
    {
        var chatbotService = new FakeChatbotService("¡Excelente! Hemos registrado tu número: 934 772 764.");
        var leadNotificationService = new FakeChatLeadNotificationService();
        var controller = new ChatApiController(
            chatbotService,
            leadNotificationService,
            NullLogger<ChatApiController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var request = new ChatApiController.SendMessageRequest("934772764", [
            new ChatMessageDto("user", "Hola, quiero un chatbot"),
            new ChatMessageDto("model", "¿Prefieres llamada o WhatsApp?")
        ]);

        var result = await controller.SendMessage(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(leadNotificationService.LastNotifiedLead);
        Assert.Equal("+51 934 772 764", leadNotificationService.LastNotifiedLead.Contact);
        Assert.Equal("https://wa.me/51934772764", leadNotificationService.LastNotifiedLead.WhatsAppUrl);
    }

    [Fact]
    public async Task SendMessage_WhenMessageHasNoContact_DoesNotNotifyLead()
    {
        var chatbotService = new FakeChatbotService("Una web cuesta desde S/. 799.");
        var leadNotificationService = new FakeChatLeadNotificationService();
        var controller = new ChatApiController(
            chatbotService,
            leadNotificationService,
            NullLogger<ChatApiController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var request = new ChatApiController.SendMessageRequest("¿Cuánto cuesta una web?", null);

        var result = await controller.SendMessage(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(leadNotificationService.LastNotifiedLead);
    }

    [Fact]
    public async Task SendMessage_WhenMessageIsEmpty_ReturnsBadRequest()
    {
        var chatbotService = new FakeChatbotService("");
        var leadNotificationService = new FakeChatLeadNotificationService();
        var controller = new ChatApiController(
            chatbotService,
            leadNotificationService,
            NullLogger<ChatApiController>.Instance);

        var result = await controller.SendMessage(new ChatApiController.SendMessageRequest("", null), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
