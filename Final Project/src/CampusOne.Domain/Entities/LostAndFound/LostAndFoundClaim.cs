using CampusOne.Domain.Enums;

namespace CampusOne.Domain.Entities.LostAndFound;

public class LostAndFoundClaim
{
    public Guid Id { get; set; }
    public Guid LostItemId { get; set; }
    public Guid FoundItemId { get; set; }
    public string ClaimantUserId { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public string? VerificationNote { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public LostAndFoundItem? LostItem { get; set; }
    public LostAndFoundItem? FoundItem { get; set; }
}