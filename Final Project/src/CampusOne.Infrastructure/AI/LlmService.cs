using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusOne.Infrastructure.AI;

public class LlmService
{
    private readonly HttpClient _httpClient;

    public LlmService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        var apiKey = configuration["OpenRouter:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
            ?? throw new InvalidOperationException("OpenRouter API key is not configured.");

        _httpClient.BaseAddress ??= new Uri("https://openrouter.ai/api/v1/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string> GenerateAnswerAsync(
        string question,
        string context,
        CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            You are a university document assistant.
            Answer using ONLY the provided context.
            Do not invent facts. If the context is insufficient, say so.
            Answer in the same language as the user's question.

            Context:
            {context}

            Question:
            {question}
            """;

        using var response = await _httpClient.PostAsJsonAsync(
            "chat/completions",
            new
            {
                model = "openrouter/free",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            },
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenRouter LLM error: {(int)response.StatusCode} - {body}");

        using var document = JsonDocument.Parse(body);

        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}
