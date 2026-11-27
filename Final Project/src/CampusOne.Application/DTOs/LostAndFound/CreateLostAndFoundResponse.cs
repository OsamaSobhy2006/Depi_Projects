using CampusOne.Domain.Enums;

namespace CampusOne.Application.DTOs.LostAndFound;

public class CreateLostAndFoundResponse
{
    public Guid ItemId { get; set; }
    public LostAndFoundStatus Status { get; set; }
    public string GuidanceMessage { get; set; } = string.Empty;
    public IReadOnlyList<MatchCandidateDto> PossibleMatches { get; set; } = Array.Empty<MatchCandidateDto>();
}