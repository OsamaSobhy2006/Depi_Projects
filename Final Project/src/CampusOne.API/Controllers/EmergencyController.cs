using CampusOne.API.Hubs;
using CampusOne.Application.DTOs.Emergency;
using CampusOne.Application.Interfaces.Emergency;
using CampusOne.Domain.Entities.Emergency.Enums;
using CampusOne.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CampusOne.API.Controllers.Emergency;

[ApiController]
[Route("[controller]")]
[Authorize]
public class EmergencyController : ControllerBase
{
    private const string ResponderRoles = "Admin,Staff";

    private readonly IEmergencyService _emergencyService;
    private readonly IHubContext<EmergencyHub> _hubContext;

    public EmergencyController(
        IEmergencyService emergencyService,
        IHubContext<EmergencyHub> hubContext)
    {
        _emergencyService = emergencyService;
        _hubContext = hubContext;
    }

    [HttpPost("trigger")]
    public async Task<ActionResult<EmergencyIncidentDto>> Trigger(
        [FromBody] TriggerEmergencyDto dto,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var created = await _emergencyService.TriggerAsync(userId, dto, ct);

        await _hubContext.Clients
            .Group(EmergencyHub.RespondersGroupName)
            .SendAsync("ReceiveEmergencyIncident", created, ct);

        await _hubContext.Clients
            .User(userId)
            .SendAsync("EmergencyIncidentCreated", created, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("active/me")]
    public async Task<ActionResult<EmergencyIncidentDto?>> GetMyActiveIncident(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var active = await _emergencyService.GetActiveForUserAsync(userId, ct);
        return Ok(active);
    }

    [HttpGet("active")]
    [Authorize(Roles = ResponderRoles)]
    public async Task<ActionResult<IReadOnlyList<EmergencyIncidentDto>>> GetActiveIncidents(CancellationToken ct)
    {
        var incidents = await _emergencyService.GetActiveIncidentsAsync(ct);
        return Ok(incidents);
    }

    [HttpGet("history")]
    [Authorize(Roles = ResponderRoles)]
    public async Task<ActionResult<IReadOnlyList<EmergencyIncidentDto>>> GetHistory(
        [FromQuery] EmergencyCategory? category,
        [FromQuery] EmergencyStatus? status,
        CancellationToken ct)
    {
        var history = await _emergencyService.GetHistoryAsync(category, status, ct);
        return Ok(history);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmergencyIncidentDto>> GetById(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var active = await _emergencyService.GetActiveForUserAsync(userId, ct);
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Staff");

        if (!isStaff && active?.Id != id)
        {
            return Forbid();
        }

        var history = await _emergencyService.GetHistoryAsync(null, null, ct);
        var incident = history.FirstOrDefault(x => x.Id == id);
        if (incident is null)
        {
            return NotFound();
        }

        return Ok(incident);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<EmergencyIncidentDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateEmergencyStatusDto dto,
        CancellationToken ct)
    {
        var actorId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return Unauthorized();
        }

        var isStaff = User.IsInRole("Admin") || User.IsInRole("Staff");
        var myActive = await _emergencyService.GetActiveForUserAsync(actorId, ct);
        var isOwnIncident = myActive?.Id == id;

        if (!isStaff && !isOwnIncident)
        {
            return Forbid();
        }

        try
        {
            var updated = await _emergencyService.UpdateStatusAsync(id, actorId, dto, ct);

            await _hubContext.Clients
                .Group(EmergencyHub.RespondersGroupName)
                .SendAsync("EmergencyIncidentUpdated", updated, ct);

            await _hubContext.Clients
                .Group(EmergencyHub.GetIncidentGroupName(id))
                .SendAsync("EmergencyIncidentUpdated", updated, ct);

            await _hubContext.Clients
                .User(updated.ReporterUserId)
                .SendAsync("EmergencyIncidentUpdated", updated, ct);

            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("location")]
    public async Task<ActionResult<EmergencyIncidentDto>> UpdateLocation(
        [FromBody] EmergencyLocationPingDto dto,
        CancellationToken ct)
    {
        var actorId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return Unauthorized();
        }

        try
        {
            var updated = await _emergencyService.UpdateLocationAsync(actorId, dto, ct);

            await _hubContext.Clients
                .Group(EmergencyHub.RespondersGroupName)
                .SendAsync("EmergencyLocationUpdated", updated, ct);

            await _hubContext.Clients
                .Group(EmergencyHub.GetIncidentGroupName(dto.IncidentId))
                .SendAsync("EmergencyLocationUpdated", updated, ct);

            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
    }
}