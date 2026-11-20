namespace CampusOne.Application.DTOs.Study;

public class CreateStudyGroupRequest
{
    public string Title { get; set; } = string.Empty;
    public string? CourseCode { get; set; }
    public string? Description { get; set; }
}

public class StudyGroupDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CourseCode { get; set; }
    public string? Description { get; set; }
    public int MembersCount { get; set; }
    public bool HasActiveSession { get; set; }
    public string? ActiveRoomCode { get; set; }
    public DateTime CreatedAt { get; set; }
}