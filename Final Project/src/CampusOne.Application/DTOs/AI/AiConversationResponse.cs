namespace CampusOne.Application.DTOs.AI
{
    public class AiConversationResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
