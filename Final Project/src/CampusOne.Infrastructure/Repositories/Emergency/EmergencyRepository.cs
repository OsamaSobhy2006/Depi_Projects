using CampusOne.Application.Interfaces.Emergency;
using CampusOne.Domain.Entities.Emergency;
using CampusOne.Domain.Entities.Emergency.Enums;
using CampusOne.Domain.Enums;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Infrastructure.Repositories.Emergency;

public class EmergencyRepository : IEmergencyRepository
{
    private readonly ApplicationDbContext _context;

    public EmergencyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmergencyIncident> AddAsync(EmergencyIncident incident, CancellationToken ct = default)
    {
        await _context.EmergencyIncidents.AddAsync(incident, ct);
        return incident;
    }

    public async Task<EmergencyIncident?> GetByIdAsync(Guid id, bool includeUpdates = true, CancellationToken ct = default)
    {
        IQueryable<EmergencyIncident> query = _context.EmergencyIncidents;

        if (includeUpdates)
        {
            query = query.Include(x => x.Updates.OrderBy(u => u.CreatedAt));
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<EmergencyIncident?> GetActiveByUserAsync(string userId, CancellationToken ct = default)
    {
        return await _context.EmergencyIncidents
            .Include(x => x.Updates.OrderBy(u => u.CreatedAt))
            .Where(x => x.ReporterUserId == userId &&
                        x.Status != EmergencyStatus.Resolved &&
                        x.Status != EmergencyStatus.FalseAlarm)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<EmergencyIncident>> GetActiveIncidentsAsync(CancellationToken ct = default)
    {
        return await _context.EmergencyIncidents
            .Include(x => x.Updates.OrderBy(u => u.CreatedAt))
            .Where(x => x.Status != EmergencyStatus.Resolved &&
                        x.Status != EmergencyStatus.FalseAlarm)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EmergencyIncident>> GetHistoryAsync(
        EmergencyCategory? category,
        EmergencyStatus? status,
        CancellationToken ct = default)
    {
        IQueryable<EmergencyIncident> query = _context.EmergencyIncidents
            .Include(x => x.Updates.OrderBy(u => u.CreatedAt));

        if (category.HasValue)
        {
            query = query.Where(x => x.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddUpdateLogAsync(EmergencyUpdateLog log, CancellationToken ct = default)
    {
        await _context.EmergencyUpdateLogs.AddAsync(log, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}