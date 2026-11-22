using CampusOne.Application.DTOs.Emergency;
using CampusOne.Domain.Entities.Emergency.Enums;
using CampusOne.Domain.Enums;

namespace CampusOne.Application.Interfaces.Emergency;

public interface IEmergencyService
{
    Task<EmergencyIncidentDto> TriggerAsync(string reporterUserId, TriggerEmergencyDto dto, CancellationToken ct = default);
    Task<EmergencyIncidentDto?> GetActiveForUserAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<EmergencyIncidentDto>> GetActiveIncidentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EmergencyIncidentDto>> GetHistoryAsync(EmergencyCategory? category, EmergencyStatus? status, CancellationToken ct = default);
    Task<EmergencyIncidentDto> UpdateStatusAsync(Guid incidentId, string actorUserId, UpdateEmergencyStatusDto dto, CancellationToken ct = default);
    Task<EmergencyIncidentDto> UpdateLocationAsync(string actorUserId, EmergencyLocationPingDto dto, CancellationToken ct = default);
}