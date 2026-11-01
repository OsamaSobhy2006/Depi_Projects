using CampusOne.Application.DTOs.AI;

namespace CampusOne.Application.Interfaces.AI
{
    public interface IAiConversationService
    {
        Task<AiConversationResponse> CreateAsync(string userId, CancellationToken cancellationToken = default);
        Task<List<AiConversationResponse>> GetAllAsync(string userId, CancellationToken cancellationToken = default);
        Task<AiConversationDetailsResponse?> GetByIdAsync(
            Guid conversationId,
            string userId,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            Guid conversationId,
            string userId,
            CancellationToken cancellationToken = default);
    }
}