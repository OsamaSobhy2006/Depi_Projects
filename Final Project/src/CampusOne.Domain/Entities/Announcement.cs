using CampusOne.Domain.Enums;

namespace CampusOne.Domain.Entities;

public class Announcement
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementCategory Category { get; set; } = AnnouncementCategory.General;
    public bool IsPinned { get; set; }

    /// <summary>Visible to students from this moment (allows scheduling).</summary>
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Hidden from students after this moment. Null = never expires.</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}