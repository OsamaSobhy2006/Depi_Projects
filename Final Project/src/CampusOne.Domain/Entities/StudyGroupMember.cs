using CampusOne.Domain.Enums;

namespace CampusOne.Domain.Entities;

public class StudyGroupMember
{
    public Guid StudyGroupId { get; set; }
    public StudyGroup StudyGroup { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public StudyGroupRole Role { get; set; } = StudyGroupRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}