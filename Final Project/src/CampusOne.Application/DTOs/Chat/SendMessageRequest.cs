using System.ComponentModel.DataAnnotations;

namespace CampusOne.Application.DTOs.Chat;

public class SendMessageRequest
{
    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;
}