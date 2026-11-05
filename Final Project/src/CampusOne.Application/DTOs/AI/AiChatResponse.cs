namespace CampusOne.Application.DTOs.AI
{
    public class AiChatResponse
    {
        public string Answer { get; set; } = string.Empty;
        public bool FoundRelevantInformation { get; set; }
        public double? BestSimilarityScore { get; set; }
    }
}
