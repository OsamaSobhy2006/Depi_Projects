using CampusOne.Domain.Enums;

namespace CampusOne.Application.DTOs.LostAndFound;

public class MatchCandidateDto
{
    public Guid MatchedItemId { get; set; }
    public LostAndFoundType MatchedItemType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public string GuidanceMessage { get; set; } = string.Empty;
}