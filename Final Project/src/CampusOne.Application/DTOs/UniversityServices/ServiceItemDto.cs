
namespace CampusOne.Application.DTOs.UniversityServices;

public class ServiceItemDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Instructions { get; set; }

    public string? Icon { get; set; }

    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}
