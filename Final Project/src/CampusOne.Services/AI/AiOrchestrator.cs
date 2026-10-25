using CampusOne.Application.DTOs.AI;
using CampusOne.Application.Interfaces.AI;

namespace CampusOne.Services.AI
{
    public class AiOrchestrator : IAiOrchestrator
    {
        private readonly IRagService _ragService;

        public AiOrchestrator(IRagService ragService)
        {
            _ragService = ragService;
        }

        public Task<AiChatResponse> AskAsync(AiChatRequest request, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(request.Question))
                throw new ArgumentException("Question cannot be empty.",nameof(request));

           return _ragService.AskAsync(request.Question.Trim(), cancellationToken);
        }
    }
}
