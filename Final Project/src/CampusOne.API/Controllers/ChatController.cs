using System.Security.Claims;
using CampusOne.Application.DTOs.Chat;
using CampusOne.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusOne.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
    }

    [HttpGet("users")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? search)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == null)
            return Unauthorized();

        var users = await _chatService.SearchUsersAsync(currentUserId, search);

        return Ok(users);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == null)
            return Unauthorized();

        if (page < 1)
            return BadRequest("Page must be greater than 0.");

        var (items, totalCount) =
            await _chatService.GetConversationsAsync(
                currentUserId,
                page,
                pageSize);

        return Ok(new
        {
            Items = items,
            Page = page,
            PageSize = Math.Clamp(pageSize, 1, 100),
            TotalCount = totalCount,
            HasNextPage = page * Math.Clamp(pageSize, 1, 100) < totalCount
        });
    }

    [HttpPost("conversations/private")]
    public async Task<IActionResult> CreatePrivateConversation(
        [FromBody] CreatePrivateConversationRequest request)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == null)
            return Unauthorized();

        try
        {
            var conversationId =
                await _chatService.CreatePrivateConversationAsync(
                    currentUserId,
                    request.UserId);

            return Ok(new { ConversationId = conversationId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("conversations/{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == null)
            return Unauthorized();

        try
        {
            var messages = await _chatService.GetMessagesAsync(
                currentUserId,
                id);

            return Ok(messages);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid id,
        [FromBody] SendMessageRequest request)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == null)
            return Unauthorized();

        try
        {
            var message = await _chatService.SendMessageAsync(
                currentUserId,
                id,
                request.Content);

            return Ok(message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}