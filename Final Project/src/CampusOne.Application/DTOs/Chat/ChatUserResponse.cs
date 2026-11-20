namespace CampusOne.Application.DTOs.Chat;

public class ChatUserResponse
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Department { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public string Role { get; set; } = string.Empty;
}