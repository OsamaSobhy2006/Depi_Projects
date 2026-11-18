
namespace CampusOne.Domain.Entities.UniversityServices;

public class ServiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Instructions { get; set; }

    public string? Icon { get; set; }

    public Guid CategoryId { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ServiceCategory Category { get; set; } = null!;

    public ICollection<ServiceRequest> Requests { get; set; }
        = new List<ServiceRequest>();
}
