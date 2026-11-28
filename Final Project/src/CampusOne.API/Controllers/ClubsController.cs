using System.Security.Claims;
using CampusOne.Application.DTOs;
using CampusOne.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusOne.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ClubsController(IClubService service) : ControllerBase
{
    private const string ClubManagerRoles = "Admin,Staff";

    private string? UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private bool IsClubManager =>
        User.IsInRole("Admin")
        || User.IsInRole("Staff")
        || User.FindAll(ClaimTypes.Role).Any(c =>
            string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Value, "Staff", StringComparison.OrdinalIgnoreCase))
        || User.FindAll("role").Any(c =>
            string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Value, "Staff", StringComparison.OrdinalIgnoreCase));

    [HttpGet]
    [ProducesResponseType(typeof(List<ClubDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ClubDto>>> GetAll([FromQuery] ClubQuery query, CancellationToken ct)
    {
        query.IncludeInactive = query.IncludeInactive && IsClubManager;
        return Ok(await service.GetAllAsync(query, UserId, ct));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClubDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClubDto>> GetById(int id, CancellationToken ct)
    {
        var club = await service.GetByIdAsync(id, UserId, includeInactive: IsClubManager, ct);
        return club is null ? NotFound() : Ok(club);
    }

    /// <summary>Clubs the current user has joined.</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(List<ClubDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ClubDto>>> GetMine(CancellationToken ct)
    {
        if (UserId is null) return Unauthorized();
        return Ok(await service.GetMyClubsAsync(UserId, ct));
    }

    [HttpPost("{id:int}/join")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join(int id, CancellationToken ct)
    {
        if (UserId is null) return Unauthorized();

        return await service.JoinAsync(id, UserId, ct) switch
        {
            JoinClubResult.Joined => NoContent(),
            JoinClubResult.ClubNotFound => NotFound(),
            JoinClubResult.AlreadyMember => Problem(statusCode: StatusCodes.Status409Conflict, title: "You are already a member of this club."),
            JoinClubResult.ClubFull => Problem(statusCode: StatusCodes.Status409Conflict, title: "This club is full."),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpDelete("{id:int}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Leave(int id, CancellationToken ct)
    {
        if (UserId is null) return Unauthorized();
        return await service.LeaveAsync(id, UserId, ct) ? NoContent() : NotFound();
    }

    [HttpPost]
    [Authorize(Roles = ClubManagerRoles)]
    [ProducesResponseType(typeof(ClubDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClubDto>> Create([FromBody] CreateClubDto dto, CancellationToken ct)
    {
        if (await service.NameExistsAsync(dto.Name, null, ct))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "A club with this name already exists.");

        var created = await service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ClubManagerRoles)]
    [ProducesResponseType(typeof(ClubDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClubDto>> Update(int id, [FromBody] UpdateClubDto dto, CancellationToken ct)
    {
        if (await service.NameExistsAsync(dto.Name, id, ct))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "A club with this name already exists.");

        var updated = await service.UpdateAsync(id, dto, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = ClubManagerRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await service.DeleteAsync(id, ct) ? NoContent() : NotFound();
}