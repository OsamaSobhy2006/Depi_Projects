namespace CampusOne.Domain.Entities.Chat
{
    public class ConversationParticipant
    {
        public Guid ConversationId { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public Conversation Conversation { get; set; } = null!;
    }
}
