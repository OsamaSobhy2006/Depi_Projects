using CampusOne.Application.DTOs.AI;
using CampusOne.Application.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CampusOne.Infrastructure.AI;

public class LlmService : IAiIntentClassifier
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



    public async Task<AiIntent> ClassifyAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            return AiIntent.OutOfScope;

        var documentIntent = DetectUniversityDocumentIntent(question);

        if (documentIntent.HasValue)
        {
            Console.WriteLine(
                $"[AI CLASSIFIER] Rule-based intent: {documentIntent.Value}");

            return documentIntent.Value;
        }

        var featureIntent = DetectFeatureIntent(question);

        if (featureIntent.HasValue)
        {
            Console.WriteLine(
                $"[AI CLASSIFIER] Rule-based intent: {featureIntent.Value}");

            return featureIntent.Value;
        }

        const string systemPrompt = """
        You are the intent classifier for CampusOne, a university platform.

        IMPORTANT CONTEXT:
        The user is interacting with CampusOne AI.
        Questions about university regulations, academic information,
        events, announcements, clubs, study groups, university services,
        and other campus features are within scope.

        Classify the user's question into exactly ONE intent:

        UniversityDocument:
        Questions about university regulations, academic rules,
        departments, courses, credit hours, grading, admission,
        graduation, examinations, and official university documents.

        Events:
        Questions about university events, upcoming events,
        today's events, event schedules, event details, and campus activities.

        Announcements:
        Questions about announcements, notices, and university updates.

        Clubs:
        Questions about student clubs, club activities, and membership.

        StudyGroups:
        Questions about study groups, study partners, and group activities.

        UniversityServices:
        Questions about university services and student requests.

        LostAndFound:
        Questions about lost or found items.

        Marketplace:
        Questions about buying, selling, and marketplace listings.

        GeneralCampusOne:
        Questions about using CampusOne, its features, and navigation.

        OutOfScope:
        Questions clearly unrelated to the university or CampusOne.

        CLASSIFICATION EXAMPLES:

        "Are there any events today?" -> Events
        "What events are coming up?" -> Events
        "What are the latest announcements?" -> Announcements
        "How can I join a student club?" -> Clubs
        "Find me a study group" -> StudyGroups
        "Where can I report a lost item?" -> LostAndFound
        "What are the scientific departments?" -> UniversityDocument
        "What are the graduation requirements?" -> UniversityDocument
        "How do I use CampusOne?" -> GeneralCampusOne
        "What is the capital of France?" -> OutOfScope

        RULES:
        1. Understand Arabic and English.
        2. Short questions are valid.
        3. Do not classify university-related questions as OutOfScope
           just because they do not explicitly mention the university.
        4. Return only one exact intent name.
        5. Do not provide explanations or additional text.
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
                    content = question
                }
                },
                temperature = 0,
                max_tokens = 30
            },
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"OpenRouter classification error: {(int)response.StatusCode} - {body}");
        }

        using var document = JsonDocument.Parse(body);

        var result = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(result))
            return AiIntent.OutOfScope;

        var normalizedResult = result.Trim()
            .Trim('`', '"', '\'', '.', ':', ' ');

        foreach (var intent in Enum.GetValues<AiIntent>())
        {
            if (normalizedResult.Contains(
                intent.ToString(),
                StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(
                    $"[AI CLASSIFIER] LLM intent: {intent}");

                return intent;
            }
        }

        Console.WriteLine(
            $"[AI CLASSIFIER] Unexpected response: {result}");

        return AiIntent.OutOfScope;
    }

    private static AiIntent? DetectFeatureIntent(string question)
    {
        var q = question.ToLowerInvariant();

        if (Regex.IsMatch(
                q,
                @"\b(events?|event's|upcoming events?|campus activities)\b")
            || q.Contains("فعاليات")
            || q.Contains("فعالية")
            || q.Contains("ايفنت")
            || q.Contains("إيفنت")
            || q.Contains("الفعاليات"))
        {
            return AiIntent.Events;
        }

        if (Regex.IsMatch(
                q,
                @"\b(announcements?|notices?|university updates)\b")
            || q.Contains("الإعلانات")
            || q.Contains("الاعلانات")
            || q.Contains("إعلان")
            || q.Contains("اعلان"))
        {
            return AiIntent.Announcements;
        }

        if (Regex.IsMatch(
                q,
                @"\b(clubs?|club membership)\b")
            || q.Contains("الأندية")
            || q.Contains("النوادي")
            || q.Contains("نادي"))
        {
            return AiIntent.Clubs;
        }

        if (Regex.IsMatch(
                q,
                @"\b(study groups?|study partners?)\b")
            || q.Contains("مجموعات الدراسة")
            || q.Contains("جروبات المذاكرة"))
        {
            return AiIntent.StudyGroups;
        }

        if (Regex.IsMatch(
                q,
                @"\b(university services|student services|service requests?)\b")
            || q.Contains("الخدمات الجامعية")
            || q.Contains("الخدمات الطلابية"))
        {
            return AiIntent.UniversityServices;
        }

        if (Regex.IsMatch(
                q,
                @"\b(lost and found|lost items?|found items?|missing items?)\b")
            || q.Contains("المفقودات")
            || q.Contains("المفقود")
            || q.Contains("العثور على"))
        {
            return AiIntent.LostAndFound;
        }

        if (Regex.IsMatch(
                q,
                @"\b(marketplace|buy|sell|selling|buying|for sale)\b")
            || q.Contains("السوق")
            || q.Contains("البيع")
            || q.Contains("الشراء"))
        {
            return AiIntent.Marketplace;
        }

        return null;
    }

    private static AiIntent? DetectUniversityDocumentIntent(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return null;

        var q = question.ToLowerInvariant();

        if (Regex.IsMatch(
            q,
            @"\b(departments?|scientific departments|academic departments|graduation requirements|graduation|credit hours?|credit hour system|academic rules|university regulations|admission requirements|examination rules|grading system|study plan|curriculum|specializations?|courses?|subjects?|faculty regulations|academic regulations)\b"))
        {
            return AiIntent.UniversityDocument;
        }

        if (q.Contains("الأقسام العلمية")
            || q.Contains("الاقسام العلمية")
            || q.Contains("أقسام الكلية")
            || q.Contains("اقسام الكلية")
            || q.Contains("شروط التخرج")
            || q.Contains("متطلبات التخرج")
            || q.Contains("الساعات المعتمدة")
            || q.Contains("نظام الساعات")
            || q.Contains("اللائحة")
            || q.Contains("اللوائح")
            || q.Contains("الامتحانات")
            || q.Contains("الامتحان")
            || q.Contains("التقديرات")
            || q.Contains("التخصصات")
            || q.Contains("المقررات الدراسية")
            || q.Contains("الخطة الدراسية")
            || q.Contains("الأقسام")
            || q.Contains("الاقسام"))
        {
            return AiIntent.UniversityDocument;
        }

        return null;
    }

}
