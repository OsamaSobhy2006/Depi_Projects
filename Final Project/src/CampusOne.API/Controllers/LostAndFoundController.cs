using CampusOne.Application.DTOs.LostAndFound;
using CampusOne.Application.Interfaces.LostAndFound;
using CampusOne.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CampusOne.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class LostAndFoundController : ControllerBase
{
    private readonly ILostAndFoundService _lostAndFoundService;

    public LostAndFoundController(ILostAndFoundService lostAndFoundService)
    {
        _lostAndFoundService = lostAndFoundService;
    }

    public class CreateReportFormRequest
    {
        public LostAndFoundType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Location { get; set; }
        public DateTime? IncidentDate { get; set; }
        public IFormFile Image { get; set; } = null!;
    }

    public class SubmitClaimBodyRequest
    {
        public Guid LostItemId { get; set; }
        public Guid FoundItemId { get; set; }
        public string? VerificationNote { get; set; }
    }

    [HttpPost("reports")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateReport(
        [FromForm] CreateReportFormRequest form,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (form.Image == null || form.Image.Length == 0)
            return BadRequest("Image file is required.");

        await using var stream = form.Image.OpenReadStream();

        var request = new CreateLostAndFoundRequest
        {
            ReporterUserId = userId,
            Type = form.Type,
            Title = form.Title,
            Category = form.Category,
            Description = form.Description,
            Location = form.Location,
            IncidentDate = form.IncidentDate ?? DateTime.UtcNow,
            ImageStream = stream,
            FileName = form.Image.FileName
        };

        var result = await _lostAndFoundService.CreateReportAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("reports/{itemId:guid}/matches")]
    public async Task<IActionResult> GetMatches(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var matches = await _lostAndFoundService.FindMatchesAsync(itemId, cancellationToken);
        return Ok(matches);
    }

    [HttpPost("claims")]
    public async Task<IActionResult> SubmitClaim(
    [FromBody] SubmitClaimBodyRequest body,
    CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var claim = await _lostAndFoundService.SubmitClaimAsync(
            body.LostItemId,
            body.FoundItemId,
            userId,
            body.VerificationNote,
            cancellationToken);

        return Ok(new
        {
            claim.Id,
            claim.LostItemId,
            claim.FoundItemId,
            claim.ClaimantUserId,
            claim.SimilarityScore,
            claim.VerificationNote,
            claim.Status,
            claim.CreatedAt
        });
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports(
    [FromQuery] LostAndFoundType? type,
    CancellationToken cancellationToken)
    {
        var items = await _lostAndFoundService.GetReportsAsync(type, cancellationToken);
        return Ok(items);
    }

    [HttpPatch("reports/{itemId:guid}/resolve")]
    public async Task<IActionResult> ResolveItem(
    Guid itemId,
    CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        try
        {
            await _lostAndFoundService.ResolveItemAsync(itemId, userId, cancellationToken);
            return Ok(new { itemId, status = "Resolved" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpDelete("reports/{itemId:guid}")]
    public async Task<IActionResult> DeleteReport(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        try
        {
            await _lostAndFoundService.DeleteReportAsync(itemId, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }
}