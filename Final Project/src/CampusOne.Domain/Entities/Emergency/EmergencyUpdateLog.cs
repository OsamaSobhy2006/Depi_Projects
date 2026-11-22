using CampusOne.Domain.Entities.Emergency.Enums;

namespace CampusOne.Domain.Entities.Emergency;

public class EmergencyUpdateLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IncidentId { get; set; }
    public EmergencyIncident? Incident { get; set; }

    public string ActorUserId { get; set; } = string.Empty;
    public EmergencyStatus? NewStatus { get; set; }
    public string? Message { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}