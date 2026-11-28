using System.ComponentModel.DataAnnotations;
using CampusOne.Domain.Enums;

namespace CampusOne.Application.DTOs;

public class AnnouncementDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementCategory Category { get; set; }
    public bool IsPinned { get; set; }
    public DateTime PublishedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

public class AnnouncementQuery
{
    public AnnouncementCategory? Category { get; set; }
    public string? Search { get; set; }

    /// <summary>Only honoured for admins: also returns scheduled and expired announcements.</summary>
    public bool IncludeInactive { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;
}

public class CreateAnnouncementDto : IValidatableObject
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    public AnnouncementCategory Category { get; set; } = AnnouncementCategory.General;
    public bool IsPinned { get; set; }

    /// <summary>Null = publish now (on create) / keep current value (on update).</summary>
    public DateTime? PublishedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExpiresAtUtc is { } expires && PublishedAtUtc is { } published && expires <= published)
            yield return new ValidationResult(
                "ExpiresAtUtc must be later than PublishedAtUtc.", new[] { nameof(ExpiresAtUtc) });
    }
}

public class UpdateAnnouncementDto : CreateAnnouncementDto
{
}