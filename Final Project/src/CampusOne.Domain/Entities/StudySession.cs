namespace CampusOne.Domain.Entities;

public class StudySession
{
    public Guid Id { get; set; }
    public Guid StudyGroupId { get; set; }
    public StudyGroup StudyGroup { get; set; } = null!;

    public string Topic { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public bool IsLive { get; set; } = true;
    public string StartedByUserId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
}