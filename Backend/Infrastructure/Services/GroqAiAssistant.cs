using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WhatsAppParser.Application.Features.Chat.Commands;
using WhatsAppParser.Application.Interfaces;

namespace WhatsAppParser.Infrastructure.Services;

public sealed class GroqAiAssistant(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GroqAiAssistant> logger) : IAiAssistant
{
    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string Model = "llama-3.3-70b-versatile";

    public async Task<string> CompleteAsync(
        string systemPrompt,
        IReadOnlyList<ChatTurn> messages,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["Groq:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Groq:ApiKey não configurada.");

        var groqMessages = new List<GroqMessage>
        {
            new("system", systemPrompt)
        };

        groqMessages.AddRange(messages.Select(t => new GroqMessage(t.Role, t.Content)));

        var body = new GroqRequest(Model, groqMessages, 2048);

        using var req = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = JsonContent.Create(body, options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        var response = await httpClient.SendAsync(req, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GroqResponse>(
            cancellationToken: cancellationToken);

        var text = result?.Choices?.FirstOrDefault()?.Message?.Content
                   ?? "(sem resposta)";

        logger.LogInformation("Groq response: {Tokens} tokens used", result?.Usage?.TotalTokens);

        return text;
    }

    // ── Internal DTOs ──────────────────────────────────────────────────────

    private sealed record GroqRequest(
        string Model,
        List<GroqMessage> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private sealed record GroqMessage(string Role, string Content);

    private sealed class GroqResponse
    {
        public List<GroqChoice>? Choices { get; set; }
        public GroqUsage? Usage { get; set; }
    }

    private sealed class GroqChoice
    {
        public GroqMessage? Message { get; set; }
    }

    private sealed class GroqUsage
    {
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
