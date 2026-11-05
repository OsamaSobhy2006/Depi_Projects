using CampusOne.Application.DTOs.AI;
using CampusOne.Application.Interfaces.AI;
using CampusOne.Domain.Entities.AI;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Services.AI
{
    public class AiConversationService : IAiConversationService
    {
        private readonly ApplicationDbContext _context;

        public AiConversationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AiConversationResponse> CreateAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var existingTitles = await _context.AiConversations
                .Where(c => c.UserId == userId)
                .Select(c => c.Title)
                .ToListAsync(cancellationToken);

            var maxNumber = existingTitles
                .Select(title =>
                {
                    const string prefix = "Chat Conversation ";

                    return title.StartsWith(prefix) &&
                           int.TryParse(title[prefix.Length..], out var number)
                        ? number
                        : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            var now = DateTime.UtcNow;

            var conversation = new AiConversation
            {
                UserId = userId,
                Title = $"Chat Conversation {maxNumber + 1}",
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.AiConversations.Add(conversation);

            await _context.SaveChangesAsync(cancellationToken);

            return new AiConversationResponse
            {
                Id = conversation.Id,
                Title = conversation.Title,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt
            };
        }

        public async Task<List<AiConversationResponse>> GetAllAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.AiConversations
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new AiConversationResponse
                {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<AiConversationDetailsResponse?> GetByIdAsync(
            Guid conversationId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.AiConversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.UserId == userId)
                .Select(c => new AiConversationDetailsResponse
                {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,

                    Messages = c.Messages
                        .OrderBy(m => m.CreatedAt)
                        .Select(m => new AiMessageResponse
                        {
                            Id = m.Id,
                            Sender = m.Sender,
                            Content = m.Content,
                            CreatedAt = m.CreatedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(
            Guid conversationId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var conversation = await _context.AiConversations
                .FirstOrDefaultAsync(
                    c => c.Id == conversationId && c.UserId == userId,
                    cancellationToken);

            if (conversation == null)
                return false;

            _context.AiConversations.Remove(conversation);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}