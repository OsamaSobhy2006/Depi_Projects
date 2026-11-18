
using CampusOne.Application.DTOs.UniversityServices;
using CampusOne.Application.Interfaces.UniversityServices;
using Microsoft.AspNetCore.Mvc;

namespace CampusOne.API.Controllers.UniversityServices;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceRepository _serviceRepository;

    public ServicesController(IServiceRepository serviceRepository)
    {
        _serviceRepository = serviceRepository;
    }


    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(
        CancellationToken cancellationToken)
    {
        var categories = await _serviceRepository
            .GetCategoriesAsync(cancellationToken);

        var result = categories.Select(category => new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Icon = category.Icon,
            DisplayOrder = category.DisplayOrder,
            ServicesCount = category.Services.Count
        });

        return Ok(result);
    }


    [HttpGet("category/{categoryId:guid}")]
    public async Task<IActionResult> GetServicesByCategory(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var services = await _serviceRepository
            .GetServicesByCategoryAsync(categoryId, cancellationToken);

        var result = services.Select(service => new ServiceItemDto
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            Instructions = service.Instructions,
            Icon = service.Icon,
            CategoryId = service.CategoryId,
            CategoryName = service.Category.Name
        });

        return Ok(result);
    }


    [HttpGet("{serviceId:guid}")]
    public async Task<IActionResult> GetServiceDetails(
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var service = await _serviceRepository
            .GetServiceByIdAsync(serviceId, cancellationToken);

        if (service == null)
        {
            return NotFound(new
            {
                message = "Service was not found."
            });
        }

        var result = new ServiceItemDto
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            Instructions = service.Instructions,
            Icon = service.Icon,
            CategoryId = service.CategoryId,
            CategoryName = service.Category.Name
        };

        return Ok(result);
    }
}
