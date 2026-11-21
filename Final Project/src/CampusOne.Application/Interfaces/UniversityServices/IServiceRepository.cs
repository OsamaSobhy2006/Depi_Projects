
using CampusOne.Domain.Entities.UniversityServices;

namespace CampusOne.Application.Interfaces.UniversityServices;

public interface IServiceRepository
{
    Task<IReadOnlyList<ServiceCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceItem>> GetServicesByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<ServiceItem?> GetServiceByIdAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default);


    Task<bool> DeactivateServiceAsync(
    Guid serviceId,
    CancellationToken cancellationToken = default);

    Task<bool> DeactivateCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);
}
