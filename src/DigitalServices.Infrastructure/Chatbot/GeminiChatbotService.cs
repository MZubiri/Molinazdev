using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DigitalServices.Application.Chatbot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalServices.Infrastructure.Chatbot;

public sealed class GeminiChatbotService(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiChatbotService> logger) : IChatbotService
{
    private readonly GeminiOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string DefaultFriendlyFallback =
        "¡Hola! Nuestro asistente con IA está experimentando una alta demanda de consultas en este momento. " +
        "Puedes agendar una asesoría técnica gratuita directamente con nuestro equipo o contactarnos vía WhatsApp al +51 983 529 230 para cotizar tu proyecto.";

    public async Task<ChatResponseDto> GenerateReplyAsync(
        ChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apiKey = !string.IsNullOrWhiteSpace(_options.ApiKey)
            ? _options.ApiKey
            : Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogError("Gemini API key is not configured.");
            return new ChatResponseDto(DefaultFriendlyFallback, "none", true);
        }

        var candidateModels = new[] { _options.PrimaryModel, _options.FallbackModel }
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var payload = BuildGeminiPayload(request);

        for (var i = 0; i < candidateModels.Count; i++)
        {
            var model = candidateModels[i];
            var isFallback = i > 0;

            try
            {
                var response = await CallGeminiModelAsync(model, apiKey, payload, cancellationToken);
                if (response is not null)
                {
                    if (isFallback)
                    {
                        logger.LogInformation("Successfully answered user query using fallback model: {Model}", model);
                    }

                    return new ChatResponseDto(response, model, isFallback);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Error while calling Gemini model {Model}. Falling back to next available model.", model);
            }
        }

        logger.LogError("All configured Gemini models failed to generate a response.");
        return new ChatResponseDto(DefaultFriendlyFallback, "fallback-static", true);
    }

    private async Task<string?> CallGeminiModelAsync(
        string model,
        string apiKey,
        GeminiRequestPayload payload,
        CancellationToken cancellationToken)
    {
        var cleanModel = model.Trim();
        if (cleanModel.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
        {
            cleanModel = cleanModel["models/".Length..];
        }

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{cleanModel}:generateContent?key={apiKey}";

        using var response = await httpClient.PostAsJsonAsync(endpoint, payload, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "Gemini model {Model} returned HTTP {StatusCode}. Response: {Response}",
                model,
                response.StatusCode,
                errorBody);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(JsonOptions, cancellationToken);
        var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private GeminiRequestPayload BuildGeminiPayload(ChatRequestDto request)
    {
        var contents = new List<GeminiContent>();

        // Include recent history (up to last 10 messages)
        if (request.History is { Count: > 0 })
        {
            var recentHistory = request.History.TakeLast(10);
            foreach (var h in recentHistory)
            {
                if (string.IsNullOrWhiteSpace(h.Content)) continue;

                var role = h.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ||
                           h.Role.Equals("model", StringComparison.OrdinalIgnoreCase) ||
                           h.Role.Equals("bot", StringComparison.OrdinalIgnoreCase)
                    ? "model"
                    : "user";

                contents.Add(new GeminiContent
                {
                    Role = role,
                    Parts = [new GeminiPart { Text = h.Content.Trim() }]
                });
            }
        }

        // Add the current user message
        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = [new GeminiPart { Text = request.Message.Trim() }]
        });

        return new GeminiRequestPayload
        {
            SystemInstruction = string.IsNullOrWhiteSpace(_options.SystemPrompt)
                ? null
                : new GeminiContent
                {
                    Parts = [new GeminiPart { Text = _options.SystemPrompt.Trim() }]
                },
            Contents = contents,
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.7f,
                MaxOutputTokens = 800
            }
        };
    }

    private sealed class GeminiRequestPayload
    {
        [JsonPropertyName("system_instruction")]
        public GeminiContent? SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; } = 0.7f;

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 800;
    }

    private sealed class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }

        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
    }
}
