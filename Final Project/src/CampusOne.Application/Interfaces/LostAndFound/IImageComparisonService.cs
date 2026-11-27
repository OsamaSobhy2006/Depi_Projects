namespace CampusOne.Application.Interfaces.LostAndFound;

public interface IImageComparisonService
{
    Task<double> CompareImagesAsync(
        string sourceImagePathOrUrl,
        string candidateImagePathOrUrl,
        CancellationToken cancellationToken = default);
}