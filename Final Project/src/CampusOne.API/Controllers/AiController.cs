using System.Security.Claims;
using CampusOne.Application.DTOs.AI;
using CampusOne.Application.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusOne.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly IAiOrchestrator _aiOrchestrator;
        private readonly IAiConversationService _conversationService;

        public AiController(IAiOrchestrator aiOrchestrator, IAiConversationService conversationService)
        {
            _aiOrchestrator = aiOrchestrator;
            _conversationService = conversationService;
        }

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        [HttpPost("chat")]
        public async Task<ActionResult<AiChatResponse>> Chat(
            [FromBody] AiChatRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var response = await _aiOrchestrator.AskAsync(
                    request, userId, cancellationToken);

                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Conversation not found." });
            }
        }

        [HttpPost("conversations")]
        public async Task<ActionResult<AiConversationResponse>> CreateConversation(
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var conversation = await _conversationService.CreateAsync(userId, cancellationToken);

            return CreatedAtAction(
                nameof(GetConversation),
                new { id = conversation.Id },
                conversation);
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<List<AiConversationResponse>>> GetConversations(
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var conversations = await _conversationService.GetAllAsync(userId, cancellationToken);

            return Ok(conversations);
        }

        [HttpGet("conversations/{id:guid}")]
        public async Task<ActionResult<AiConversationDetailsResponse>> GetConversation(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var conversation = await _conversationService.GetByIdAsync(
                id, userId, cancellationToken);

            if (conversation == null)
                return NotFound(new { message = "Conversation not found." });

            return Ok(conversation);
        }

        [HttpDelete("conversations/{id:guid}")]
        public async Task<IActionResult> DeleteConversation(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var deleted = await _conversationService.DeleteAsync(
                id, userId, cancellationToken);

            if (!deleted)
                return NotFound(new { message = "Conversation not found." });

            return NoContent();
        }
    }
}