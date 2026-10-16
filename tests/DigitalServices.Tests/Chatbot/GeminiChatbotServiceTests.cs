using System.Net;
using DigitalServices.Application.Chatbot;
using DigitalServices.Infrastructure.Chatbot;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalServices.Tests.Chatbot;

public sealed class GeminiChatbotServiceTests
{
    private sealed class DelegatingHandlerStub(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => handler(request);
    }

    [Fact]
    public async Task GenerateReplyAsync_WhenApiKeyMissing_ReturnsFallbackImmediately()
    {
        var options = Options.Create(new GeminiOptions { ApiKey = "" });
        var client = new HttpClient(new DelegatingHandlerStub(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));
        var service = new GeminiChatbotService(client, options, NullLogger<GeminiChatbotService>.Instance);

        var response = await service.GenerateReplyAsync(new ChatRequestDto("Hola"));

        Assert.True(response.IsFallback);
        Assert.Contains("asistente con IA", response.Reply);
    }

    [Fact]
    public async Task GenerateReplyAsync_WhenPrimarySucceeds_ReturnsPrimaryResult()
    {
        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "test-key",
            PrimaryModel = "gemini-3.5-flash",
            FallbackModel = "gemini-3.6-flash"
        });

        var client = new HttpClient(new DelegatingHandlerStub(req =>
        {
            var json = """
            {
              "candidates": [
                {
                  "content": {
                    "parts": [{ "text": "¡Hola! ¿En qué puedo ayudarte?" }]
                  }
                }
              ]
            }
            """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }));

        var service = new GeminiChatbotService(client, options, NullLogger<GeminiChatbotService>.Instance);

        var response = await service.GenerateReplyAsync(new ChatRequestDto("Hola"));

        Assert.False(response.IsFallback);
        Assert.Equal("gemini-3.5-flash", response.ModelUsed);
        Assert.Equal("¡Hola! ¿En qué puedo ayudarte?", response.Reply);
    }

    [Fact]
    public async Task GenerateReplyAsync_WhenPrimaryFails_FallsBackToSecondaryModel()
    {
        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "test-key",
            PrimaryModel = "gemini-3.5-flash",
            FallbackModel = "gemini-3.6-flash"
        });

        var callCount = 0;
        var client = new HttpClient(new DelegatingHandlerStub(req =>
        {
            callCount++;
            if (callCount == 1)
            {
                // Primary model returns 503 Overloaded
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            // Fallback model returns 200 OK
            var json = """
            {
              "candidates": [
                {
                  "content": {
                    "parts": [{ "text": "Respuesta desde el modelo de respaldo." }]
                  }
                }
              ]
            }
            """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }));

        var service = new GeminiChatbotService(client, options, NullLogger<GeminiChatbotService>.Instance);

        var response = await service.GenerateReplyAsync(new ChatRequestDto("Hola"));

        Assert.True(response.IsFallback);
        Assert.Equal("gemini-3.6-flash", response.ModelUsed);
        Assert.Equal("Respuesta desde el modelo de respaldo.", response.Reply);
        Assert.Equal(2, callCount);
    }
}
