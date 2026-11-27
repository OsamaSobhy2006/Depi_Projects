using CampusOne.Domain.Enums;

namespace CampusOne.Domain.Entities.LostAndFound;

public class LostAndFoundItem
{
    public Guid Id { get; set; }
    public string ReporterUserId { get; set; } = string.Empty;
    public LostAndFoundType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime IncidentDate { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public LostAndFoundStatus Status { get; set; } = LostAndFoundStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LostAndFoundClaim> Claims { get; set; } = new List<LostAndFoundClaim>();
}