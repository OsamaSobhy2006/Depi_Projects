namespace CampusOne.Application.DTOs.Chat;

public class ConversationResponse
{
    public Guid Id { get; set; }

    public string OtherUserId { get; set; } = string.Empty;

    public string OtherUserName { get; set; } = string.Empty;

    public string? OtherUserProfilePictureUrl { get; set; }

    public string OtherUserRole { get; set; } = string.Empty;

    public string? LastMessage { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public int UnreadCount { get; set; }
}