
using CampusOne.Application.Interfaces.UniversityServices;
using CampusOne.Domain.Entities.UniversityServices;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Infrastructure.Repositories.UniversityServices;

public class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly ApplicationDbContext _context;

    public ServiceRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        await _context.ServiceRequests.AddAsync(
            request,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequest>> GetStudentRequestsAsync(
        string studentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceRequests
            .AsNoTracking()
            .Include(r => r.ServiceItem)
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceRequest?> GetByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceRequests
            .Include(r => r.ServiceItem)
            .FirstOrDefaultAsync(
                r => r.Id == requestId,
                cancellationToken);
    }

    public async Task<ServiceRequest?> GetByIdForStudentAsync(
        Guid requestId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceRequests
            .AsNoTracking()
            .Include(r => r.ServiceItem)
            .FirstOrDefaultAsync(
                r => r.Id == requestId &&
                     r.StudentId == studentId,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
