using CampusOne.Application.DTOs.AI;

namespace CampusOne.Application.Interfaces.AI
{
    public interface IAiOrchestrator
    {
        Task<AiChatResponse> AskAsync(
            AiChatRequest request, CancellationToken cancellationToken = default);
    }
}
