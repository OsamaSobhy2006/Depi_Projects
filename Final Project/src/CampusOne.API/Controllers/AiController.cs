using CampusOne.Application.DTOs.AI;
using CampusOne.Application.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusOne.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class AiController : Controller
    {
        private readonly IAiOrchestrator _aiOrchestrator;
        public AiController(IAiOrchestrator aiOrchestrator)
        {
            _aiOrchestrator = aiOrchestrator;
        }

        [HttpPost("chat")]
        public async Task<ActionResult<AiChatResponse>> Chat(
            [FromBody]AiChatRequest request,
            CancellationToken cancellationToken = default)
        {
            var response = await _aiOrchestrator.AskAsync(request, cancellationToken);

            return Ok(response);
        }
    }
}
