using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusOne.Infrastructure.AI
{
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _embeddingModel;

        public EmbeddingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            var apiKey = configuration["Gemini:ApiKey"]
                ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? configuration["OpenRouter:ApiKey"]
                ?? Environment.GetEnvironmentVariable("OpenRouter_API_KEY")
                ?? throw new InvalidOperationException("AI API key is not configured.");

            var baseUrl = configuration["Gemini:BaseUrl"]
                ?? Environment.GetEnvironmentVariable("GEMINI_BASE_URL")
                ?? "https://generativelanguage.googleapis.com/v1beta/openai/";

            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";

            _embeddingModel = configuration["Gemini:EmbeddingModel"]
                ?? Environment.GetEnvironmentVariable("GEMINI_EMBEDDING_MODEL")
                ?? "gemini-embedding-001";

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<float[]> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "embeddings",
                new
                {
                    model = _embeddingModel,
                    input = text
                },
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Embedding API error: {(int)response.StatusCode} - {body}");

            using var document = JsonDocument.Parse(body);

            return document.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();
        }
    }
}