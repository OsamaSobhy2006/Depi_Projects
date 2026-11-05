namespace CampusOne.Application.DTOs.AI
{
    public class AiMessageResponse
    {
        public Guid Id { get; set; }
        public string Sender { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
