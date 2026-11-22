using CampusOne.Domain.Entities.Emergency.Enums;
using CampusOne.Domain.Enums;

namespace CampusOne.Application.DTOs.Emergency;

public class TriggerEmergencyDto
{
    public EmergencyCategory Category { get; set; }
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public string? LocationLabel { get; set; }
}

public class UpdateEmergencyStatusDto
{
    public EmergencyStatus Status { get; set; }
    public string? AssignedResponderId { get; set; }
    public string? Message { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class EmergencyLocationPingDto
{
    public Guid IncidentId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public string? LocationLabel { get; set; }
}

public class EmergencyUpdateLogDto
{
    public Guid Id { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public EmergencyStatus? NewStatus { get; set; }
    public string? Message { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ActorName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EmergencyIncidentDto
{
    public Guid Id { get; set; }
    public string ReporterUserId { get; set; } = string.Empty;
    public string? AssignedResponderId { get; set; }
    public string? ReporterName { get; set; }
    public string? AssignedResponderName { get; set; }
    public EmergencyCategory Category { get; set; }
    public EmergencyStatus Status { get; set; }
    public string? Description { get; set; }
    public string? AiSuggestedCategory { get; set; }
    public string? AiStructuredSummary { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public string? LocationLabel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondingAt { get; set; }
    public DateTime? InvestigatingAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public double? ResponseTimeSeconds { get; set; }
    public List<EmergencyUpdateLogDto> Updates { get; set; } = new();
}