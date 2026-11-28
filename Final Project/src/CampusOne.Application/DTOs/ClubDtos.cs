using System.ComponentModel.DataAnnotations;
using CampusOne.Domain.Enums;

namespace CampusOne.Application.DTOs;

public class ClubDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ClubCategory Category { get; set; }
    public string? LogoUrl { get; set; }
    public string? MeetingInfo { get; set; }
    public int? MaxMembers { get; set; }
    public int MemberCount { get; set; }
    public bool IsMember { get; set; }
    public bool IsFull { get; set; }
}

public class ClubQuery
{
    public ClubCategory? Category { get; set; }
    public string? Search { get; set; }

    /// <summary>Only honoured for admins: also returns inactive clubs.</summary>
    public bool IncludeInactive { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;
}

public class CreateClubDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public ClubCategory Category { get; set; } = ClubCategory.Academic;

    [StringLength(500), Url]
    public string? LogoUrl { get; set; }

    [StringLength(200)]
    public string? MeetingInfo { get; set; }

    [Range(1, 100000)]
    public int? MaxMembers { get; set; }
}

public class UpdateClubDto : CreateClubDto
{
    public bool IsActive { get; set; } = true;
}

public enum JoinClubResult
{
    Joined,
    AlreadyMember,
    ClubNotFound,
    ClubFull
}