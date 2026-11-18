
namespace CampusOne.Application.DTOs.UniversityServices;

public class CategoryDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Icon { get; set; }

    public int DisplayOrder { get; set; }

    public int ServicesCount { get; set; }
}
