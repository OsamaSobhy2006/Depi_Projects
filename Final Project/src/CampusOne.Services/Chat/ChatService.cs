using CampusOne.Application.DTOs.Chat;
using CampusOne.Application.Interfaces;
using CampusOne.Domain.Entities.Chat;
using CampusOne.Infrastructure.Identity;
using CampusOne.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Services.Chat;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<ChatUserResponse>> SearchUsersAsync(
        string currentUserId,
        string? search)
    {
        var allowedRoles = new[] { "Student", "Professor", "Staff" };

        var users = new List<ApplicationUser>();

        foreach (var role in allowedRoles)
        {
            var roleUsers = await _userManager.GetUsersInRoleAsync(role);
            users.AddRange(roleUsers);
        }

        var uniqueUsers = users
            .DistinctBy(u => u.Id)
            .Where(u => u.Id != currentUserId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            uniqueUsers = uniqueUsers.Where(u =>
                (u.FirstName + " " + u.LastName)
                    .Contains(term, StringComparison.OrdinalIgnoreCase)
                || (u.Email ?? "").Contains(
                    term,
                    StringComparison.OrdinalIgnoreCase));
        }

        var result = new List<ChatUserResponse>();

        foreach (var user in uniqueUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new ChatUserResponse
            {
                Id = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? string.Empty,
                Department = user.Department,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Role = roles.FirstOrDefault() ?? "User"
            });
        }

        return result.OrderBy(u => u.FullName).ToList();
    }

    public async Task<(List<ConversationResponse> Items, int TotalCount)>
        GetConversationsAsync(
            string currentUserId,
            int page = 1,
            int pageSize = 20)
    {
        if (page < 1)
            throw new ArgumentException("Invalid page number.");

        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Conversations
            .AsNoTracking()
            .Where(c => c.Participants.Any(
                p => p.UserId == currentUserId));

        var totalCount = await query.CountAsync();

        var conversations = await query
            .OrderByDescending(c => c.UpdatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,

                OtherUserId = c.Participants
                    .Where(p => p.UserId != currentUserId)
                    .Select(p => p.UserId)
                    .FirstOrDefault(),

                LastMessage = c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .ThenByDescending(m => m.Id)
                    .Select(m => new
                    {
                        m.Content,
                        m.CreatedAt
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var result = new List<ConversationResponse>();

        foreach (var conversation in conversations)
        {
            if (conversation.OtherUserId == null)
                continue;

            var otherUser = await _userManager.FindByIdAsync(
                conversation.OtherUserId);

            if (otherUser == null)
                continue;

            var roles = await _userManager.GetRolesAsync(otherUser);

            result.Add(new ConversationResponse
            {
                Id = conversation.Id,
                OtherUserId = otherUser.Id,
                OtherUserName = $"{otherUser.FirstName} {otherUser.LastName}",
                OtherUserProfilePictureUrl = otherUser.ProfilePictureUrl,
                OtherUserRole = roles.FirstOrDefault() ?? "User",
                LastMessage = conversation.LastMessage?.Content,
                LastMessageAt = conversation.LastMessage?.CreatedAt,
                UnreadCount = 0
            });
        }

        return (result, totalCount);
    }

    public async Task<Guid> CreatePrivateConversationAsync(
        string currentUserId,
        string otherUserId)
    {
        if (currentUserId == otherUserId)
            throw new InvalidOperationException(
                "You cannot create a conversation with yourself.");

        var otherUser = await _userManager.FindByIdAsync(otherUserId);

        if (otherUser == null)
            throw new KeyNotFoundException("User not found.");

        var existingConversationId = await _context.Conversations
            .Where(c =>
                c.Participants.Count == 2 &&
                c.Participants.Any(p => p.UserId == currentUserId) &&
                c.Participants.Any(p => p.UserId == otherUserId))
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        if (existingConversationId != Guid.Empty)
            return existingConversationId;

        var now = DateTime.UtcNow;

        var conversation = new Conversation
        {
            CreatedAt = now,
            UpdatedAt = now,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = currentUserId },
                new() { UserId = otherUserId }
            }
        };

        _context.Conversations.Add(conversation);

        await _context.SaveChangesAsync();

        return conversation.Id;
    }

    public async Task<List<MessageResponse>> GetMessagesAsync(
        string currentUserId,
        Guid conversationId)
    {
        var isParticipant = await _context.ConversationParticipants
            .AnyAsync(p =>
                p.ConversationId == conversationId &&
                p.UserId == currentUserId);

        if (!isParticipant)
            throw new UnauthorizedAccessException(
                "You are not a participant in this conversation.");

        var messages = await _context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Select(m => new MessageResponse
            {
                Id = m.Id,
                SenderId = m.SenderId,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                SenderName = _context.Users
                    .Where(u => u.Id == m.SenderId)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault() ?? "Unknown User"
            })
            .ToListAsync();

        return messages;
    }

    public async Task<MessageResponse> SendMessageAsync(
        string currentUserId,
        Guid conversationId,
        string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message cannot be empty.");

        content = content.Trim();

        if (content.Length > 5000)
            throw new ArgumentException(
                "Message cannot exceed 5000 characters.");

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
            throw new KeyNotFoundException("Conversation not found.");

        var isParticipant = await _context.ConversationParticipants
            .AnyAsync(p =>
                p.ConversationId == conversationId &&
                p.UserId == currentUserId);

        if (!isParticipant)
            throw new UnauthorizedAccessException(
                "You are not allowed to send messages in this conversation.");

        var sender = await _userManager.FindByIdAsync(currentUserId);

        if (sender == null)
            throw new KeyNotFoundException("Sender not found.");

        var now = DateTime.UtcNow;

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = currentUserId,
            Content = content,
            CreatedAt = now
        };

        _context.Messages.Add(message);

        conversation.UpdatedAt = now;

        await _context.SaveChangesAsync();

        return new MessageResponse
        {
            Id = message.Id,
            SenderId = sender.Id,
            SenderName = $"{sender.FirstName} {sender.LastName}",
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };
    }
}