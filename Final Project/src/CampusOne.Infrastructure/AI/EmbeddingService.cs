using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusOne.Infrastructure.AI
{
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;

        public EmbeddingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            var apiKey = configuration["OpenRouter:ApiKey"]
                ?? Environment.GetEnvironmentVariable("OpenRouter_API_KEY")
                ?? throw new InvalidOperationException("OpenRouter API key is not configured");

            _httpClient.BaseAddress ??= new Uri("https://openrouter.ai/api/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        }
    
        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "embeddings",
                new
                {
                    model = "nvidia/nemotron-3-embed-1b:free",
                    input = text
                }, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"OpenRouter embedding error: {(int)response.StatusCode} - {body}");

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
