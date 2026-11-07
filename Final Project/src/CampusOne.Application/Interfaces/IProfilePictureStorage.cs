namespace CampusOne.Application.Interfaces
{
    public interface IProfilePictureStorage
    {
        Task SaveAsync(string userId, Stream fileStream, string extension, CancellationToken cancellationToken = default);

        Task<Stream?> OpenReadAsync(string userId, CancellationToken cancellationToken = default);
    }
}
