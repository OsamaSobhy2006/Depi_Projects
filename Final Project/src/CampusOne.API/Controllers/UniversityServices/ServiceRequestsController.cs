
using System.Security.Claims;
using CampusOne.Application.DTOs.UniversityServices;
using CampusOne.Application.Interfaces.UniversityServices;
using CampusOne.Domain.Entities.UniversityServices;
using CampusOne.Domain.Entities.UniversityServices.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusOne.API.Controllers.UniversityServices;

[ApiController]
[Route("/[controller]")]
[Authorize]
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestRepository _requestRepository;
    private readonly IServiceRepository _serviceRepository;

    public ServiceRequestsController(
        IServiceRequestRepository requestRepository,
        IServiceRepository serviceRepository)
    {
        _requestRepository = requestRepository;
        _serviceRepository = serviceRepository;
    }

    // POST: /ServiceRequests
    [HttpPost]
    public async Task<IActionResult> CreateRequest(
        [FromBody] CreateServiceRequestDto dto,
        CancellationToken cancellationToken)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var service = await _serviceRepository.GetServiceByIdAsync(
            dto.ServiceItemId, cancellationToken);

        if (service == null)
            return NotFound(new { message = "Service not found." });

        if (!Enum.IsDefined(typeof(Priority), dto.Priority))
            return BadRequest(new { message = "Invalid priority." });

        var request = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = $"SR-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}",
            StudentId = studentId,
            ServiceItemId = dto.ServiceItemId,
            Subject = dto.Subject.Trim(),
            Description = dto.Description.Trim(),
            Priority = dto.Priority,
            Status = RequestStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        await _requestRepository.AddAsync(request, cancellationToken);
        await _requestRepository.SaveChangesAsync(cancellationToken);

        var response = new ServiceRequestResponseDto
        {
            Id = request.Id,
            RequestNumber = request.RequestNumber,
            ServiceItemId = service.Id,
            ServiceName = service.Name,
            Subject = request.Subject,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            SubmittedAt = request.SubmittedAt
        };

        return CreatedAtAction(
            nameof(GetMyRequest),
            new { requestId = request.Id },
            response);
    }

    // GET: /ServiceRequests/my-requests
    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyRequests(
        CancellationToken cancellationToken)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var requests = await _requestRepository.GetStudentRequestsAsync(
            studentId, cancellationToken);

        var result = requests.Select(MapToResponse);

        return Ok(result);
    }

    // GET: /ServiceRequests/{requestId}
    [HttpGet("{requestId:guid}")]
    public async Task<IActionResult> GetMyRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var request = await _requestRepository.GetByIdForStudentAsync(
            requestId, studentId, cancellationToken);

        if (request == null)
            return NotFound(new { message = "Request not found." });

        return Ok(MapToResponse(request));
    }

    // PUT: /ServiceRequests/{requestId}/status
    [HttpPut("{requestId:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        Guid requestId,
        [FromBody] UpdateServiceRequestStatusDto dto,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(RequestStatus), dto.Status))
            return BadRequest(new { message = "Invalid request status." });

        var request = await _requestRepository.GetByIdAsync(
            requestId, cancellationToken);

        if (request == null)
            return NotFound(new { message = "Request not found." });

        request.Status = dto.Status;
        request.AdminNotes = dto.AdminNotes?.Trim();
        request.UpdatedAt = DateTime.UtcNow;

        if (dto.Status == RequestStatus.Completed)
            request.CompletedAt = DateTime.UtcNow;
        else
            request.CompletedAt = null;

        await _requestRepository.SaveChangesAsync(cancellationToken);

        return Ok(MapToResponse(request));
    }

    // DELETE: /ServiceRequests/{requestId}
    [HttpDelete("{requestId:guid}")]
    public async Task<IActionResult> DeleteRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var studentId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var deleted = await _requestRepository
            .DeleteStudentRequestAsync(
                requestId,
                studentId,
                cancellationToken);

        if (!deleted)
        {
            return BadRequest(new
            {
                message = "Request not found or cannot be cancelled."
            });
        }

        return Ok(new
        {
            message = "Request cancelled successfully."
        });
    }

    private static ServiceRequestResponseDto MapToResponse(
        ServiceRequest request)
    {
        return new ServiceRequestResponseDto
        {
            Id = request.Id,
            RequestNumber = request.RequestNumber,
            ServiceItemId = request.ServiceItemId,
            ServiceName = request.ServiceItem.Name,
            Subject = request.Subject,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            AdminNotes = request.AdminNotes,
            SubmittedAt = request.SubmittedAt,
            UpdatedAt = request.UpdatedAt,
            CompletedAt = request.CompletedAt
        };
    }


}
