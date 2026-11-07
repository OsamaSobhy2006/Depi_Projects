using CampusOne.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CampusOne.Infrastructure.Services
{
    public class LocalProfilePictureStorage : IProfilePictureStorage
    {
        private readonly string _storagePath;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        public LocalProfilePictureStorage(IConfiguration configuration)
        {
            _storagePath = configuration["FileStorage:ProfilePicturesPath"]
                ?? Path.Combine(
                    AppContext.BaseDirectory,
                    "App_Data",
                    "ProfilePictures");

            Directory.CreateDirectory(_storagePath);
        }

        public async Task SaveAsync(
            string userId,
            Stream fileStream,
            string extension,
            CancellationToken cancellationToken = default)
        {
            foreach (var oldExtension in AllowedExtensions)
            {
                var oldFilePath = Path.Combine(
                    _storagePath,
                    $"{userId}{oldExtension}");

                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }

            var fileName = $"{userId}{extension}";
            var filePath = Path.Combine(_storagePath, fileName);

            await using var outputStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            await fileStream.CopyToAsync(
                outputStream,
                cancellationToken);
        }

        public Task<Stream?> OpenReadAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            foreach (var extension in AllowedExtensions)
            {
                var filePath = Path.Combine(
                    _storagePath,
                    $"{userId}{extension}");

                if (File.Exists(filePath))
                {
                    Stream stream = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read);

                    return Task.FromResult<Stream?>(stream);
                }
            }

            return Task.FromResult<Stream?>(null);
        }
    }
}
