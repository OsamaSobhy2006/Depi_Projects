using CampusOne.Domain.Entities.Emergency.Enums;
using CampusOne.Domain.Enums;

namespace CampusOne.Application.Interfaces.Emergency;

public class EmergencyTriageResult
{
    public string SuggestedCategory { get; set; } = string.Empty;
    public string StructuredSummary { get; set; } = string.Empty;
}

public interface IEmergencyTriageService
{
    Task<EmergencyTriageResult> AnalyzeAsync(
        EmergencyCategory selectedCategory,
        string? description,
        string? locationLabel,
        CancellationToken ct = default);
}