using CampusOne.Domain.Enums;

namespace CampusOne.Application.DTOs.LostAndFound;

public class CreateLostAndFoundRequest
{
    public string ReporterUserId { get; set; } = string.Empty;
    public LostAndFoundType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime IncidentDate { get; set; }
    public Stream ImageStream { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
}