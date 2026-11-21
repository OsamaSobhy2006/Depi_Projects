
using CampusOne.Application.Interfaces.UniversityServices;
using CampusOne.Domain.Entities.UniversityServices;

namespace CampusOne.Services.UniversityServices;

public class ServiceService
{
    private readonly IServiceRepository _serviceRepository;

    public ServiceService(IServiceRepository serviceRepository)
    {
        _serviceRepository = serviceRepository;
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _serviceRepository.GetCategoriesAsync(
            cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceItem>> GetServicesByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Invalid category ID.");

        return await _serviceRepository.GetServicesByCategoryAsync(
            categoryId,
            cancellationToken);
    }

    public async Task<ServiceItem?> GetServiceByIdAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        if (serviceId == Guid.Empty)
            throw new ArgumentException("Invalid service ID.");

        return await _serviceRepository.GetServiceByIdAsync(
            serviceId,
            cancellationToken);
    }
}
