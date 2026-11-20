using System.Security.Claims;
using CampusOne.Application.Interfaces;
using CampusOne.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;
    private readonly IChatService _chatService;

    public ChatHub(
        ApplicationDbContext context,
        IChatService chatService)
    {
        _context = context;
        _chatService = chatService;
    }

    private string? GetCurrentUserId()
    {
        return Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");
    }

    private static string GetConversationGroupName(Guid conversationId)
    {
        return $"conversation_{conversationId}";
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();

        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new HubException("User is not authenticated.");
        }

        var isParticipant = await _context.ConversationParticipants
            .AsNoTracking()
            .AnyAsync(p =>
                p.ConversationId == conversationId &&
                p.UserId == currentUserId);

        if (!isParticipant)
            throw new HubException("You are not allowed to join this conversation.");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetConversationGroupName(conversationId));
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetConversationGroupName(conversationId));
    }

    public async Task SendMessage(Guid conversationId, string content)
    {
        var currentUserId = GetCurrentUserId();

        if (string.IsNullOrEmpty(currentUserId))
            throw new HubException("User is not authenticated.");

        try
        {
            var message = await _chatService.SendMessageAsync(
                currentUserId,
                conversationId,
                content);

            await Clients.Group(
                GetConversationGroupName(conversationId)
            ).SendAsync("ReceiveMessage", message);
        }
        catch (UnauthorizedAccessException)
        {
            throw new HubException("You are not allowed to send messages in this conversation.");
        }
        catch (KeyNotFoundException)
        {
            throw new HubException("Conversation not found.");
        }
        catch (ArgumentException ex)
        {
            throw new HubException(ex.Message);
        }
    }
}
