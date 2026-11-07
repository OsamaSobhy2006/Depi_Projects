using CampusOne.Application.DTOs.AI;

namespace CampusOne.Application.Interfaces.AI
{
    public interface IAiIntentClassifier
    {
        Task<AiIntent> ClassifyAsync(string question, CancellationToken cancellationToken = default);
    }
}
