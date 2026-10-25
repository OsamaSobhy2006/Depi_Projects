using CampusOne.Application.DTOs.AI;

namespace CampusOne.Application.Interfaces.AI
{
    public interface IRagService
    {
        Task<AiChatResponse> AskAsync(
            string question, CancellationToken cancellationToken= default);
    }
}
