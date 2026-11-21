
using System.ComponentModel.DataAnnotations;
using CampusOne.Domain.Entities.UniversityServices.Enums;

namespace CampusOne.Application.DTOs.UniversityServices;

public class CreateServiceRequestDto
{
    [Required]
    public Guid ServiceItemId { get; set; }

    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(3000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    public Priority Priority { get; set; } = Priority.Normal;
}
