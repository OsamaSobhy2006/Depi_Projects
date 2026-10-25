using CampusOne.Application.DTOs.AI;
using CampusOne.Application.Interfaces.AI;

namespace CampusOne.Infrastructure.AI;

public class RagService : IRagService
{
    private const double MinSimilarity = 0.30;

    private readonly EmbeddingService _embeddingService;
    private readonly EmbeddingStore _store;
    private readonly SimilarityService _similarityService;
    private readonly LlmService _llmService;

    public RagService(
        EmbeddingService embeddingService,
        EmbeddingStore store,
        SimilarityService similarityService,
        LlmService llmService)
    {
        _embeddingService = embeddingService;
        _store = store;
        _similarityService = similarityService;
        _llmService = llmService;
    }

    public async Task<AiChatResponse> AskAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question, cancellationToken);

        var results = _similarityService.Search(questionEmbedding, _store.Chunks, topK: 5);

        var best = results.FirstOrDefault();

        if (best.Chunk is null || best.Score < MinSimilarity)
        {
            return new AiChatResponse
            {
                Answer = "I can only answer questions related to the university document.",
                FoundRelevantInformation = false,
                BestSimilarityScore = best.Chunk is null ? null : best.Score
            };
        }

        var context = string.Join("\n\n---\n\n",
            results
                .Where(result => result.Score >= MinSimilarity)
                .Select(result => result.Chunk.Content));

        var answer = await _llmService.GenerateAnswerAsync(question, context, cancellationToken);

        return new AiChatResponse
        {
            Answer = answer,
            FoundRelevantInformation = true,
            BestSimilarityScore = best.Score
        };
    }
}
