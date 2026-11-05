using CampusOne.Application.DTOs.AI;
using CampusOne.Application.Interfaces.AI;
using CampusOne.Domain.Entities.AI;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Services.AI
{
    public class AiOrchestrator : IAiOrchestrator
    {
        private readonly IRagService _ragService;
        private readonly ApplicationDbContext _context;

        public AiOrchestrator(IRagService ragService, ApplicationDbContext context)
        {
            _ragService = ragService;
            _context = context;
        }

        public async Task<AiChatResponse> AskAsync(
            AiChatRequest request,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                throw new ArgumentException("Question cannot be empty.", nameof(request));

            var conversation = await _context.AiConversations
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.UserId == userId,cancellationToken);

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

            var response = await _ragService.AskAsync(
                question, cancellationToken);

            var assistantMessage = new AiMessage
            {
                ConversationId = conversation.Id,
                Sender = "assistant",
                Content = response.Answer,
                CreatedAt = DateTime.UtcNow
            };

            _context.AiMessages.Add(assistantMessage);

            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return response;
        }
    }
}