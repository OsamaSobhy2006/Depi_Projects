
using System.ComponentModel.DataAnnotations;
using CampusOne.Domain.Entities.UniversityServices.Enums;

namespace CampusOne.Application.DTOs.UniversityServices;

public class UpdateServiceRequestStatusDto
{
    [Required]
    public RequestStatus Status { get; set; }

    [StringLength(2000)]
    public string? AdminNotes { get; set; }
}
