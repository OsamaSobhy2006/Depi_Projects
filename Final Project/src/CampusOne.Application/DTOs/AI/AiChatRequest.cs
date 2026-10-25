using System.ComponentModel.DataAnnotations;

namespace CampusOne.Application.DTOs.AI
{
    public class AiChatRequest
    {
        [Required]
        [MinLength(1)]
        public string Question { get; set; } = string.Empty;
    }
}
