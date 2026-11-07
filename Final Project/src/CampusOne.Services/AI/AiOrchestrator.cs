using CampusOne.Application.DTOs.AI;
using CampusOne.Application.Interfaces.AI;
using CampusOne.Domain.Entities.AI;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace CampusOne.Services.AI;

public class AiOrchestrator : IAiOrchestrator
{
    private readonly IRagService _ragService;
    private readonly IAiIntentClassifier _intentClassifier;
    private readonly ApplicationDbContext _context;
    public AiOrchestrator(
            IRagService ragService,
            IAiIntentClassifier intentClassifier,
            ApplicationDbContext context)
    {
        _ragService = ragService;
        _intentClassifier = intentClassifier;
        _context = context;
    }
    public async Task<AiChatResponse> AskAsync(
            AiChatRequest request,
            string userId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            throw new ArgumentException(
                "Question cannot be empty.",
                nameof(request));
        var conversation = await _context.AiConversations
                    .FirstOrDefaultAsync(
                        c => c.Id == request.ConversationId
                             && c.UserId == userId,
                        cancellationToken);
        if (conversation == null)
            throw new KeyNotFoundException("Conversation not found.");
        var question = request.Question.Trim();
        var userMessage = new AiMessage
        {
            ConversationId = conversation.Id,
            Sender = "user",
            Content = question,
            CreatedAt = DateTime.UtcNow
        };
        _context.AiMessages.Add(userMessage);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        var intent = await _intentClassifier.ClassifyAsync(
                    question,
                    cancellationToken);
        var result = await HandleIntentAsync(
                    intent,
                    question,
                    cancellationToken);
        var assistantMessage = new AiMessage
        {
            ConversationId = conversation.Id,
            Sender = "assistant",
            Content = result.Answer,
            CreatedAt = DateTime.UtcNow
        };
        _context.AiMessages.Add(assistantMessage);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }
    private async Task<AiChatResponse> HandleIntentAsync(
            AiIntent intent,
            string question,
            CancellationToken cancellationToken)
    {
        switch (intent)
        {
            case AiIntent.UniversityDocument:
                return await _ragService.AskAsync(
                    question,
                    cancellationToken);
            case AiIntent.Events:
                return FeatureNotAvailable("Events");
            case AiIntent.Announcements:
                return FeatureNotAvailable("Announcements");
            case AiIntent.Clubs:
                return FeatureNotAvailable("Clubs");
            case AiIntent.StudyGroups:
                return FeatureNotAvailable("Study Groups");
            case AiIntent.UniversityServices:
                return FeatureNotAvailable("University Services");
            case AiIntent.LostAndFound:
                return FeatureNotAvailable("Lost and Found");
            case AiIntent.Marketplace:
                return FeatureNotAvailable("Marketplace");
            case AiIntent.GeneralCampusOne:
                return new AiChatResponse
                {
                    Answer = "I can help you with university information and CampusOne features. Please ask me about a specific feature or university-related topic.",
                    FoundRelevantInformation = false
                };
            default:
                return new AiChatResponse
                {
                    Answer = "I can only help with university-related questions and CampusOne features.",
                    FoundRelevantInformation = false
                };
        }
    }
    private static AiChatResponse FeatureNotAvailable(string feature)
    {
        return new AiChatResponse
        {
            Answer = $"{feature} information is not available yet.",
            FoundRelevantInformation = false
        };
    }
}