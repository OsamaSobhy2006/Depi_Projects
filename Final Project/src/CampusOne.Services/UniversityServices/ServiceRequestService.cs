
using CampusOne.Application.Interfaces.UniversityServices;
using CampusOne.Domain.Entities.UniversityServices;
using CampusOne.Domain.Entities.UniversityServices.Enums;

namespace CampusOne.Services.UniversityServices;

public class ServiceRequestService
{
    private readonly IServiceRequestRepository _requestRepository;
    private readonly IServiceRepository _serviceRepository;

    public ServiceRequestService(
        IServiceRequestRepository requestRepository,
        IServiceRepository serviceRepository)
    {
        _requestRepository = requestRepository;
        _serviceRepository = serviceRepository;
    }

    public async Task<ServiceRequest> CreateRequestAsync(
        string studentId,
        Guid serviceItemId,
        string subject,
        string description,
        Priority priority = Priority.Normal,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            throw new ArgumentException("Student ID is required.");

        if (serviceItemId == Guid.Empty)
            throw new ArgumentException("Invalid service ID.");

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.");

        if (!Enum.IsDefined(priority))
            throw new ArgumentException("Invalid priority.");

        var service = await _serviceRepository.GetServiceByIdAsync(
            serviceItemId,
            cancellationToken);

        if (service == null)
            throw new InvalidOperationException("Service not found or inactive.");

        var request = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = $"USR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}",
            StudentId = studentId,
            ServiceItemId = serviceItemId,
            Subject = subject.Trim(),
            Description = description.Trim(),
            Priority = priority,
            Status = RequestStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        await _requestRepository.AddAsync(request, cancellationToken);

        await _requestRepository.SaveChangesAsync(cancellationToken);

        return request;
    }

    public async Task<IReadOnlyList<ServiceRequest>> GetStudentRequestsAsync(
        string studentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            throw new ArgumentException("Student ID is required.");

        return await _requestRepository.GetStudentRequestsAsync(
            studentId,
            cancellationToken);
    }

    public async Task<ServiceRequest?> GetRequestDetailsAsync(
        Guid requestId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        if (requestId == Guid.Empty)
            throw new ArgumentException("Invalid request ID.");

        if (string.IsNullOrWhiteSpace(studentId))
            throw new ArgumentException("Student ID is required.");

        return await _requestRepository.GetByIdForStudentAsync(
            requestId,
            studentId,
            cancellationToken);
    }
}
