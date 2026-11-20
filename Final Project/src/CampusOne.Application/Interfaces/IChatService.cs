using CampusOne.Application.DTOs.Chat;

namespace CampusOne.Application.Interfaces;

public interface IChatService
{
    Task<List<ChatUserResponse>> SearchUsersAsync(
        string currentUserId,
        string? search);

    Task<(List<ConversationResponse> Items, int TotalCount)> GetConversationsAsync(
        string currentUserId,
        int page = 1,
        int pageSize = 20);

    Task<Guid> CreatePrivateConversationAsync(
        string currentUserId,
        string otherUserId);

    Task<List<MessageResponse>> GetMessagesAsync(
        string currentUserId,
        Guid conversationId);

    Task<MessageResponse> SendMessageAsync(
        string currentUserId,
        Guid conversationId,
        string content);
}