
using CampusOne.Domain.Entities.UniversityServices;

namespace CampusOne.Application.Interfaces.UniversityServices;

public interface IServiceRequestRepository
{
    Task AddAsync(
        ServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequest>> GetStudentRequestsAsync(
        string studentId,
        CancellationToken cancellationToken = default);

    Task<ServiceRequest?> GetByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<ServiceRequest?> GetByIdForStudentAsync(
        Guid requestId,
        string studentId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<bool> DeleteStudentRequestAsync(
    Guid requestId,
    string studentId,
    CancellationToken cancellationToken = default);
}
