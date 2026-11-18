
using CampusOne.Domain.Entities.UniversityServices.Enums;

namespace CampusOne.Application.DTOs.UniversityServices;

public class ServiceRequestResponseDto
{
    public Guid Id { get; set; }

    public string RequestNumber { get; set; } = string.Empty;

    public Guid ServiceItemId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public RequestStatus Status { get; set; }

    public Priority Priority { get; set; }

    public string? AdminNotes { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
