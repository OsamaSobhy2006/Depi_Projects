using System.Globalization;
using System.Text;
using System.Text.Json;
using CampusOne.Application.Interfaces.LostAndFound;
using Microsoft.Extensions.Configuration;

namespace CampusOne.Infrastructure.LostAndFound;

public class ImageComparisonService : IImageComparisonService
{
    private readonly HttpClient _httpClient;
    private readonly string? _geminiApiKey;
    private readonly string _visionModel;

    public ImageComparisonService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _geminiApiKey = configuration["Gemini:ApiKey"]
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        _visionModel = configuration["Gemini:ChatModel"] ?? "gemini-2.5-flash";
    }

    public async Task<double> CompareImagesAsync(
        string sourceImagePathOrUrl,
        string candidateImagePathOrUrl,
        CancellationToken cancellationToken = default)
    {
        var sourceBytes = await ReadImageBytesAsync(sourceImagePathOrUrl, cancellationToken);
        var candidateBytes = await ReadImageBytesAsync(candidateImagePathOrUrl, cancellationToken);

        if (sourceBytes.Length == 0 || candidateBytes.Length == 0)
            return 0.0;

        if (!string.IsNullOrWhiteSpace(_geminiApiKey))
        {
            return await CompareWithGeminiVisionAsync(
                sourceBytes,
                candidateBytes,
                cancellationToken);
        }

        return ComputeByteDistributionSimilarity(sourceBytes, candidateBytes);
    }

    private async Task<byte[]> ReadImageBytesAsync(
        string pathOrUrl,
        CancellationToken cancellationToken)
    {
        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return await _httpClient.GetByteArrayAsync(uri, cancellationToken);
        }

        var relative = pathOrUrl.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);

        var candidatePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", relative),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CampusOne.API", "wwwroot", relative)
        };

        foreach (var path in candidatePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }

        return Array.Empty<byte>();
    }

    private async Task<double> CompareWithGeminiVisionAsync(
        byte[] imageA,
        byte[] imageB,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_visionModel}:generateContent?key={_geminiApiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                text = "Compare these two lost-and-found item images and estimate how likely they show the exact same physical item. Reply with ONLY a single number between 0.0 and 1.0 (for example: 0.85)."
                            },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = Convert.ToBase64String(imageA)
                                }
                            },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = Convert.ToBase64String(imageB)
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.0,
                    maxOutputTokens = 64
                }
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync(url, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ComputeByteDistributionSimilarity(imageA, imageB);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
                || candidates.ValueKind != JsonValueKind.Array
                || candidates.GetArrayLength() == 0)
            {
                return ComputeByteDistributionSimilarity(imageA, imageB);
            }

            var firstCandidate = candidates[0];
            if (!firstCandidate.TryGetProperty("content", out var contentElement)
                || !contentElement.TryGetProperty("parts", out var parts)
                || parts.ValueKind != JsonValueKind.Array
                || parts.GetArrayLength() == 0)
            {
                return ComputeByteDistributionSimilarity(imageA, imageB);
            }

            string? text = null;
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textElement)
                    && textElement.ValueKind == JsonValueKind.String)
                {
                    var candidateText = textElement.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(candidateText))
                    {
                        text = candidateText;
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(text))
                return ComputeByteDistributionSimilarity(imageA, imageB);

            var cleaned = text
                .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (double.TryParse(
                    cleaned,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var score))
            {
                return Math.Clamp(score, 0.0, 1.0);
            }

            return ComputeByteDistributionSimilarity(imageA, imageB);
        }
        catch
        {
            return ComputeByteDistributionSimilarity(imageA, imageB);
        }
    }

    private static double ComputeByteDistributionSimilarity(byte[] a, byte[] b)
    {
        var histA = new int[256];
        var histB = new int[256];

        foreach (var byteVal in a) histA[byteVal]++;
        foreach (var byteVal in b) histB[byteVal]++;

        double dot = 0;
        double normA = 0;
        double normB = 0;

        for (var i = 0; i < 256; i++)
        {
            dot += histA[i] * histB[i];
            normA += histA[i] * histA[i];
            normB += histB[i] * histB[i];
        }

        if (normA == 0 || normB == 0)
            return 0.0;

        return Math.Clamp(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)), 0.0, 1.0);
    }
}