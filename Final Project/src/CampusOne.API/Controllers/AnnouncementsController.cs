using System.Security.Claims;
using CampusOne.Application.DTOs;
using CampusOne.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusOne.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class AnnouncementsController(IAnnouncementService service) : ControllerBase
{
    private const string PublisherRoles = "Admin,Professor,Staff";

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private bool IsAdmin =>
        User.IsInRole("Admin")
        || User.FindAll(ClaimTypes.Role).Any(c => string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase))
        || User.FindAll("role").Any(c => string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase));

    /// <summary>Visible announcements, pinned first then newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AnnouncementDto>>> GetAll(
        [FromQuery] AnnouncementQuery query, CancellationToken ct)
    {
        query.IncludeInactive = query.IncludeInactive && IsAdmin;
        return Ok(await service.GetAllAsync(query, ct));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnnouncementDto>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, includeInactive: IsAdmin, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = PublisherRoles)]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AnnouncementDto>> Create([FromBody] CreateAnnouncementDto dto, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var created = await service.CreateAsync(dto, userId, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = PublisherRoles)]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnnouncementDto>> Update(
        int id, [FromBody] UpdateAnnouncementDto dto, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        try
        {
            var updated = await service.UpdateAsync(id, dto, userId, IsAdmin, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = PublisherRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        try
        {
            return await service.DeleteAsync(id, userId, IsAdmin, ct) ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}