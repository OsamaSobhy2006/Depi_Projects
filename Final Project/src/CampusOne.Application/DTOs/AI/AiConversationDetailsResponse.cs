namespace CampusOne.Application.DTOs.AI
{
    public class AiConversationDetailsResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<AiMessageResponse> Messages { get; set; } = new();
    }
}
