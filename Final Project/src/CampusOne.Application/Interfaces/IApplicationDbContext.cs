
namespace CampusOne.Application.Interfaces;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
