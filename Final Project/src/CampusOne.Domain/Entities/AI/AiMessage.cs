namespace CampusOne.Domain.Entities.AI
{
    public class AiMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ConversationId { get; set; }
        public string Sender { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public AiConversation Conversation { get; set; } = null!;
    }
}
