namespace CampusOne.Application.DTOs.Study;

public class StartStudySessionRequest
{
    public string Topic { get; set; } = string.Empty;
}

public class StudySessionDto
{
    public Guid Id { get; set; }
    public Guid StudyGroupId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public bool IsLive { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}