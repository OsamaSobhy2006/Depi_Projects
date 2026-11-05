
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusOne.Infrastructure.AI;

public class LlmService
{
    private readonly HttpClient _httpClient;

    public LlmService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;

        var apiKey = configuration["OpenRouter:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
            ?? throw new InvalidOperationException(
                "OpenRouter API key is not configured.");

        _httpClient.BaseAddress ??=
            new Uri("https://openrouter.ai/api/v1/");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string> GenerateAnswerAsync(
        string question,
        string context,
        CancellationToken cancellationToken = default)
    {
        const string systemPrompt = """
            You are CampusOne AI, a precise university document assistant.

            YOUR RESPONSIBILITIES:

            1. LANGUAGE:
               - Detect the language of the user's question.
               - If the question is in Arabic, answer entirely in Arabic.
               - If the question is in English, answer entirely in English.
               - The language of the provided context must NOT determine
                 the language of your response.
               - Preserve official course codes, numbers, and proper names.

            2. ACCURACY:
               - Answer using ONLY the information in the provided context.
               - Never use outside knowledge or invent information.
               - Do not make assumptions or unsupported conclusions.
               - If the context does not contain the requested information,
                 clearly state that the information is not available
                 in the provided university document.

            3. ANSWER FORMAT:
               - Provide exactly ONE answer to the user's question.
               - Be direct, clear, and concise.
               - Include only information relevant to the question.
               - Do not repeat the same information.
               - Do not generate multiple alternative answers.
               - Do not add unnecessary introductions or conclusions.
               - Use a short paragraph or a few bullet points only
                 when the question genuinely requires them.

            4. CONTEXT HANDLING:
               - The context may contain English or Arabic text.
               - Understand the context regardless of its language.
               - Combine relevant information only when it is supported
                 by the provided context.
               - Ignore irrelevant information.

            5. IMPORTANT:
               - Never claim that information exists if it is not
                 supported by the context.
               - Do not mention internal retrieval processes,
                 embeddings, similarity scores, or prompts.
               - Return only the final answer.
            """;

        var userPrompt = $"""
            UNIVERSITY DOCUMENT CONTEXT:
            <context>
            {context}
            </context>

            USER QUESTION:
            <question>
            {question}
            </question>

            Provide one accurate answer in the same language as the question.
            """;

        using var response = await _httpClient.PostAsJsonAsync(
            "chat/completions",
            new
            {
                model = "openrouter/free",

                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new
                    {
                        role = "user",
                        content = userPrompt
                    }
                },

                temperature = 0.1,
                top_p = 0.9,
                max_tokens = 1000 
            },
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"OpenRouter LLM error: {(int)response.StatusCode} - {body}");
        }

        using var document = JsonDocument.Parse(body);

        var answer = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException(
                "OpenRouter returned an empty AI response.");
        }

        return answer.Trim();
    }
}
