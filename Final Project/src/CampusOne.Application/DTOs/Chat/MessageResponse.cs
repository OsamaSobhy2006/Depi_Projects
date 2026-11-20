namespace CampusOne.Application.DTOs.Chat;

public class MessageResponse
{
    public Guid Id { get; set; }

    public string SenderId { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}