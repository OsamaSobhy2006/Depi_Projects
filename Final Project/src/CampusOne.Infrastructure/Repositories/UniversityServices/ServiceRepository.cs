
using CampusOne.Application.Interfaces.UniversityServices;
using CampusOne.Domain.Entities.UniversityServices;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Infrastructure.Repositories.UniversityServices;

public class ServiceRepository : IServiceRepository
{
    private readonly ApplicationDbContext _context;

    public ServiceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceCategories
         .AsNoTracking()
         .Include(c => c.Services)
         .Where(c => c.IsActive)
         .OrderBy(c => c.DisplayOrder)
         .ThenBy(c => c.Name)
         .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceItem>> GetServicesByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceItems
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.CategoryId == categoryId && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceItem?> GetServiceByIdAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceItems
            .AsNoTracking()
            .Include(s => s.Category)
            .FirstOrDefaultAsync(
                s => s.Id == serviceId && s.IsActive,
                cancellationToken);
    }

    public async Task<bool> DeactivateServiceAsync(
    Guid serviceId,
    CancellationToken cancellationToken = default)
    {
        var service = await _context.ServiceItems
            .FirstOrDefaultAsync(
                s => s.Id == serviceId && s.IsActive,
                cancellationToken);

        if (service == null)
            return false;

        service.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeactivateCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await _context.ServiceCategories
            .Include(c => c.Services)
            .FirstOrDefaultAsync(
                c => c.Id == categoryId && c.IsActive,
                cancellationToken);

        if (category == null)
            return false;

        category.IsActive = false;

        foreach (var service in category.Services)
        {
            service.IsActive = false;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
