using System.ComponentModel.DataAnnotations;

namespace CampusOne.Application.DTOs.Chat;

public class CreatePrivateConversationRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
}