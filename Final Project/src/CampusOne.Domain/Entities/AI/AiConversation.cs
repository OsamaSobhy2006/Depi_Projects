namespace CampusOne.Domain.Entities.AI
{
    public class AiConversation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
    }
}
