using CampusOne.Application.DTOs.Emergency;
using CampusOne.Application.Interfaces.Emergency;
using CampusOne.Domain.Entities.Emergency;
using CampusOne.Domain.Entities.Emergency.Enums;
using CampusOne.Domain.Enums;
using CampusOne.Infrastructure.Identity;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Services.Emergency;

public class EmergencyService : IEmergencyService
{
    private readonly IEmergencyRepository _repository;
    private readonly IEmergencyTriageService _triageService;
    private readonly ApplicationDbContext _dbContext;

    public EmergencyService(
        IEmergencyRepository repository,
        IEmergencyTriageService triageService,
        ApplicationDbContext dbContext)
    {
        _repository = repository;
        _triageService = triageService;
        _dbContext = dbContext;
    }

    public async Task<EmergencyIncidentDto> TriggerAsync(
        string reporterUserId,
        TriggerEmergencyDto dto,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var triage = await _triageService.AnalyzeAsync(
            dto.Category,
            dto.Description,
            dto.LocationLabel,
            ct);

        var incident = new EmergencyIncident
        {
            Id = Guid.NewGuid(),
            ReporterUserId = reporterUserId,
            Category = dto.Category,
            Status = EmergencyStatus.Created,
            Description = dto.Description?.Trim(),
            AiSuggestedCategory = triage.SuggestedCategory,
            AiStructuredSummary = triage.StructuredSummary,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            AccuracyMeters = dto.AccuracyMeters,
            LocationLabel = dto.LocationLabel?.Trim(),
            CreatedAt = now
        };

        var initialLog = new EmergencyUpdateLog
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            ActorUserId = reporterUserId,
            NewStatus = EmergencyStatus.Created,
            Message = string.IsNullOrWhiteSpace(dto.Description)
                ? "Emergency SOS triggered."
                : $"SOS triggered: {dto.Description.Trim()}",
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            CreatedAt = now
        };

        incident.Updates.Add(initialLog);

        await _repository.AddAsync(incident, ct);
        await _repository.SaveChangesAsync(ct);

        var userNames = await BuildUserNamesMapAsync(new[] { incident }, ct);
        return MapToDto(incident, userNames);
    }

    public async Task<EmergencyIncidentDto?> GetActiveForUserAsync(string userId, CancellationToken ct = default)
    {
        var incident = await _repository.GetActiveByUserAsync(userId, ct);
        if (incident is null)
        {
            return null;
        }

        var userNames = await BuildUserNamesMapAsync(new[] { incident }, ct);
        return MapToDto(incident, userNames);
    }

    public async Task<IReadOnlyList<EmergencyIncidentDto>> GetActiveIncidentsAsync(CancellationToken ct = default)
    {
        var incidents = await _repository.GetActiveIncidentsAsync(ct);
        var userNames = await BuildUserNamesMapAsync(incidents, ct);
        return incidents.Select(inc => MapToDto(inc, userNames)).ToList();
    }

    public async Task<IReadOnlyList<EmergencyIncidentDto>> GetHistoryAsync(
        EmergencyCategory? category,
        EmergencyStatus? status,
        CancellationToken ct = default)
    {
        var incidents = await _repository.GetHistoryAsync(category, status, ct);
        var userNames = await BuildUserNamesMapAsync(incidents, ct);
        return incidents.Select(inc => MapToDto(inc, userNames)).ToList();
    }

    public async Task<EmergencyIncidentDto> UpdateStatusAsync(
        Guid incidentId,
        string actorUserId,
        UpdateEmergencyStatusDto dto,
        CancellationToken ct = default)
    {
        var incident = await _repository.GetByIdAsync(incidentId, includeUpdates: true, ct)
            ?? throw new KeyNotFoundException($"Emergency incident '{incidentId}' was not found.");

        var isReporter = string.Equals(incident.ReporterUserId, actorUserId, StringComparison.Ordinal);

        if (isReporter && dto.Status != EmergencyStatus.FalseAlarm && dto.Status != EmergencyStatus.Resolved)
        {
            throw new UnauthorizedAccessException("Reporters can only close or cancel their own incident.");
        }

        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.AssignedResponderId))
        {
            incident.AssignedResponderId = dto.AssignedResponderId.Trim();
        }
        else if (dto.Status == EmergencyStatus.Responding && string.IsNullOrWhiteSpace(incident.AssignedResponderId))
        {
            incident.AssignedResponderId = actorUserId;
        }

        incident.Status = dto.Status;

        if (dto.Status == EmergencyStatus.Responding && !incident.RespondingAt.HasValue)
        {
            incident.RespondingAt = now;
        }
        else if (dto.Status == EmergencyStatus.Investigating && !incident.InvestigatingAt.HasValue)
        {
            if (!incident.RespondingAt.HasValue)
            {
                incident.RespondingAt = now;
            }
            incident.InvestigatingAt = now;
        }
        else if ((dto.Status == EmergencyStatus.Resolved || dto.Status == EmergencyStatus.FalseAlarm) && !incident.ResolvedAt.HasValue)
        {
            if (!incident.RespondingAt.HasValue)
            {
                incident.RespondingAt = now;
            }
            incident.ResolvedAt = now;
        }

        if (!string.IsNullOrWhiteSpace(dto.ResolutionNotes))
        {
            incident.ResolutionNotes = dto.ResolutionNotes.Trim();
        }

        var log = new EmergencyUpdateLog
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            ActorUserId = actorUserId,
            NewStatus = dto.Status,
            Message = !string.IsNullOrWhiteSpace(dto.Message)
                ? dto.Message.Trim()
                : !string.IsNullOrWhiteSpace(dto.ResolutionNotes)
                    ? dto.ResolutionNotes.Trim()
                    : $"Status updated to {dto.Status}.",
            Latitude = incident.Latitude,
            Longitude = incident.Longitude,
            CreatedAt = now
        };

        await _repository.AddUpdateLogAsync(log, ct);
        await _repository.SaveChangesAsync(ct);

        var updated = await _repository.GetByIdAsync(incidentId, includeUpdates: true, ct) ?? incident;
        var userNames = await BuildUserNamesMapAsync(new[] { updated }, ct);
        return MapToDto(updated, userNames);
    }

    public async Task<EmergencyIncidentDto> UpdateLocationAsync(
        string actorUserId,
        EmergencyLocationPingDto dto,
        CancellationToken ct = default)
    {
        var incident = await _repository.GetByIdAsync(dto.IncidentId, includeUpdates: true, ct)
            ?? throw new KeyNotFoundException($"Emergency incident '{dto.IncidentId}' was not found.");

        var now = DateTime.UtcNow;

        incident.Latitude = dto.Latitude;
        incident.Longitude = dto.Longitude;
        incident.AccuracyMeters = dto.AccuracyMeters;

        if (!string.IsNullOrWhiteSpace(dto.LocationLabel))
        {
            incident.LocationLabel = dto.LocationLabel.Trim();
        }

        var log = new EmergencyUpdateLog
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            ActorUserId = actorUserId,
            NewStatus = null,
            Message = !string.IsNullOrWhiteSpace(dto.LocationLabel)
                ? $"Location updated: {dto.LocationLabel.Trim()}"
                : "Location updated.",
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            CreatedAt = now
        };

        await _repository.AddUpdateLogAsync(log, ct);
        await _repository.SaveChangesAsync(ct);

        var updated = await _repository.GetByIdAsync(dto.IncidentId, includeUpdates: true, ct) ?? incident;
        var userNames = await BuildUserNamesMapAsync(new[] { updated }, ct);
        return MapToDto(updated, userNames);
    }

    private async Task<Dictionary<string, string>> BuildUserNamesMapAsync(
        IEnumerable<EmergencyIncident> incidents,
        CancellationToken ct)
    {
        var userIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var inc in incidents)
        {
            if (!string.IsNullOrWhiteSpace(inc.ReporterUserId))
            {
                userIds.Add(inc.ReporterUserId);
            }

            if (!string.IsNullOrWhiteSpace(inc.AssignedResponderId))
            {
                userIds.Add(inc.AssignedResponderId);
            }

            foreach (var u in inc.Updates)
            {
                if (!string.IsNullOrWhiteSpace(u.ActorUserId))
                {
                    userIds.Add(u.ActorUserId);
                }
            }
        }

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (userIds.Count == 0)
        {
            return map;
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);

        foreach (var id in userIds)
        {
            var found = users.FirstOrDefault(u => u.Id == id);
            map[id] = ResolveUserDisplayName(found, id);
        }

        return map;
    }

    private static string ResolveUserDisplayName(ApplicationUser? user, string fallbackId)
    {
        if (user is null)
        {
            return fallbackId;
        }

        var fullNameProp = user.GetType().GetProperty("FullName")?.GetValue(user) as string;
        if (!string.IsNullOrWhiteSpace(fullNameProp))
        {
            return fullNameProp.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user.UserName))
        {
            return user.UserName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            return user.Email.Trim();
        }

        return fallbackId;
    }

    private static EmergencyIncidentDto MapToDto(
        EmergencyIncident incident,
        IReadOnlyDictionary<string, string> userNames)
    {
        double? responseTimeSeconds = incident.RespondingAt.HasValue
            ? Math.Max(0, (incident.RespondingAt.Value - incident.CreatedAt).TotalSeconds)
            : null;

        var reporterName = userNames.TryGetValue(incident.ReporterUserId, out var rName)
            ? rName
            : incident.ReporterUserId;

        var responderName = !string.IsNullOrWhiteSpace(incident.AssignedResponderId) &&
                            userNames.TryGetValue(incident.AssignedResponderId, out var aName)
            ? aName
            : incident.AssignedResponderId;

        return new EmergencyIncidentDto
        {
            Id = incident.Id,
            ReporterUserId = incident.ReporterUserId,
            ReporterName = reporterName,
            AssignedResponderId = incident.AssignedResponderId,
            AssignedResponderName = responderName,
            Category = incident.Category,
            Status = incident.Status,
            Description = incident.Description,
            AiSuggestedCategory = incident.AiSuggestedCategory,
            AiStructuredSummary = incident.AiStructuredSummary,
            Latitude = incident.Latitude,
            Longitude = incident.Longitude,
            AccuracyMeters = incident.AccuracyMeters,
            LocationLabel = incident.LocationLabel,
            CreatedAt = incident.CreatedAt,
            RespondingAt = incident.RespondingAt,
            InvestigatingAt = incident.InvestigatingAt,
            ResolvedAt = incident.ResolvedAt,
            ResolutionNotes = incident.ResolutionNotes,
            ResponseTimeSeconds = responseTimeSeconds,
            Updates = incident.Updates
                .OrderBy(u => u.CreatedAt)
                .Select(u => new EmergencyUpdateLogDto
                {
                    Id = u.Id,
                    ActorUserId = u.ActorUserId,
                    ActorName = userNames.TryGetValue(u.ActorUserId, out var actorName)
                        ? actorName
                        : u.ActorUserId,
                    NewStatus = u.NewStatus,
                    Message = u.Message,
                    Latitude = u.Latitude,
                    Longitude = u.Longitude,
                    CreatedAt = u.CreatedAt
                })
                .ToList()
        };
    }
}