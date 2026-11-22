using CampusOne.Domain.Entities.Emergency;
using CampusOne.Domain.Entities.Emergency.Enums;
using CampusOne.Domain.Enums;

namespace CampusOne.Application.Interfaces.Emergency;

public interface IEmergencyRepository
{
    Task<EmergencyIncident> AddAsync(EmergencyIncident incident, CancellationToken ct = default);
    Task<EmergencyIncident?> GetByIdAsync(Guid id, bool includeUpdates = true, CancellationToken ct = default);
    Task<EmergencyIncident?> GetActiveByUserAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<EmergencyIncident>> GetActiveIncidentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EmergencyIncident>> GetHistoryAsync(EmergencyCategory? category, EmergencyStatus? status, CancellationToken ct = default);
    Task AddUpdateLogAsync(EmergencyUpdateLog log, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}