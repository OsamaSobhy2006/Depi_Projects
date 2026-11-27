using CampusOne.Application.DTOs.LostAndFound;
using CampusOne.Domain.Entities.LostAndFound;
using CampusOne.Domain.Enums;

namespace CampusOne.Application.Interfaces.LostAndFound;

public interface ILostAndFoundService
{
    Task<CreateLostAndFoundResponse> CreateReportAsync(
        CreateLostAndFoundRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LostAndFoundItem>> GetReportsAsync(
        LostAndFoundType? type = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchCandidateDto>> FindMatchesAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    Task<LostAndFoundClaim> SubmitClaimAsync(
        Guid lostItemId,
        Guid foundItemId,
        string claimantUserId,
        string? verificationNote,
        CancellationToken cancellationToken = default);

    Task ResolveItemAsync(
        Guid itemId,
        string userId,
        CancellationToken cancellationToken = default);

    Task DeleteReportAsync(
        Guid itemId,
        string userId,
        CancellationToken cancellationToken = default);
}