using CampusOne.Domain.Enums;

namespace CampusOne.Domain.Entities;

public class Club
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ClubCategory Category { get; set; } = ClubCategory.Academic;
    public string? LogoUrl { get; set; }
    public string? MeetingInfo { get; set; }

    /// <summary>Null = unlimited.</summary>
    public int? MaxMembers { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ClubMembership> Memberships { get; set; } = new List<ClubMembership>();
}