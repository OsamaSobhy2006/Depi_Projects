using CampusOne.Application.Interfaces.Emergency;
using CampusOne.Domain.Entities.Emergency.Enums;
using CampusOne.Domain.Enums;

namespace CampusOne.Services.Emergency;

public class EmergencyTriageService : IEmergencyTriageService
{
    public Task<EmergencyTriageResult> AnalyzeAsync(
        EmergencyCategory selectedCategory,
        string? description,
        string? locationLabel,
        CancellationToken ct = default)
    {
        var cleanDesc = description?.Trim() ?? string.Empty;
        var textLower = cleanDesc.ToLowerInvariant();

        var suggestedCategory = selectedCategory;

        if (ContainsAny(textLower, "fire", "smoke", "burn", "حريق", "دخان", "نار"))
        {
            suggestedCategory = EmergencyCategory.Fire;
        }
        else if (ContainsAny(textLower, "faint", "bleed", "injury", "heart", "medical", "إغماء", "نزيف", "إصابة", "طبي", "تعب"))
        {
            suggestedCategory = EmergencyCategory.Medical;
        }
        else if (ContainsAny(textLower, "harass", "threat", "stalk", "attack", "danger", "تحرش", "تهديد", "خطر", "ملاحقة"))
        {
            suggestedCategory = EmergencyCategory.PersonalDanger;
        }
        else if (ContainsAny(textLower, "theft", "steal", "fight", "security", "سرقة", "خناقة", "أمن"))
        {
            suggestedCategory = EmergencyCategory.Security;
        }
        else if (ContainsAny(textLower, "elevator", "electric", "water", "leak", "facility", "أسانسير", "كهرباء", "مياه", "مرفق"))
        {
            suggestedCategory = EmergencyCategory.Facility;
        }

        var locPart = string.IsNullOrWhiteSpace(locationLabel)
            ? "GPS / Campus coordinates provided"
            : locationLabel.Trim();

        var descPart = string.IsNullOrWhiteSpace(cleanDesc)
            ? "No extra text description provided by reporter."
            : cleanDesc;

        var summary = $"Category: {suggestedCategory} | Location: {locPart} | Details: {descPart}";

        return Task.FromResult(new EmergencyTriageResult
        {
            SuggestedCategory = suggestedCategory.ToString(),
            StructuredSummary = summary
        });
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        foreach (var word in keywords)
        {
            if (source.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}