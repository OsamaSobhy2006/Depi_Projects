namespace CampusOne.Domain.Entities;

public class StudyGroup
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CourseCode { get; set; }
    public string? Description { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StudyGroupMember> Members { get; set; } = new List<StudyGroupMember>();
    public ICollection<StudySession> Sessions { get; set; } = new List<StudySession>();
}