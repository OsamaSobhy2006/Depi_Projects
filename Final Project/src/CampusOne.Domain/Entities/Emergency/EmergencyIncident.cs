using CampusOne.Domain.Entities.Emergency.Enums;
using CampusOne.Domain.Enums;

namespace CampusOne.Domain.Entities.Emergency;

public class EmergencyIncident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReporterUserId { get; set; } = string.Empty;
    public string? AssignedResponderId { get; set; }

    public EmergencyCategory Category { get; set; }
    public EmergencyStatus Status { get; set; } = EmergencyStatus.Created;

    public string? Description { get; set; }
    public string? AiSuggestedCategory { get; set; }
    public string? AiStructuredSummary { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public string? LocationLabel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondingAt { get; set; }
    public DateTime? InvestigatingAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }

    public ICollection<EmergencyUpdateLog> Updates { get; set; } = new List<EmergencyUpdateLog>();
}