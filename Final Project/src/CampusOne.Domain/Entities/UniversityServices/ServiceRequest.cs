
using CampusOne.Domain.Entities.UniversityServices.Enums;

namespace CampusOne.Domain.Entities.UniversityServices;

public class ServiceRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RequestNumber { get; set; } = string.Empty;

    // Identity user ID of the student
    public string StudentId { get; set; } = string.Empty;

    public Guid ServiceItemId { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public Priority Priority { get; set; } = Priority.Normal;

    public string? AdminNotes { get; set; }

    public string? AssignedTo { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public ServiceItem ServiceItem { get; set; } = null!;
}
