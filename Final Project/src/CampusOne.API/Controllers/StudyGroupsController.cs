using System.Security.Claims;
using CampusOne.Application.DTOs.Study;
using CampusOne.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusOne.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class StudyGroupsController : ControllerBase
{
    private readonly IStudyGroupService _studyGroupService;

    public StudyGroupsController(IStudyGroupService studyGroupService)
    {
        _studyGroupService = studyGroupService;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("User ID not found in token.");

    // GET: studygroups?query=algorithms&courseCode=CS301&onlyLive=false
    [HttpGet]
    public async Task<ActionResult<List<StudyGroupDto>>> GetGroups(
        [FromQuery] string? query = null,
        [FromQuery] string? courseCode = null,
        [FromQuery] bool onlyLive = false)
    {
        var groups = await _studyGroupService.GetGroupsAsync(query, courseCode, onlyLive);
        return Ok(groups);
    }

    // POST: studygroups
    [HttpPost]
    public async Task<ActionResult<StudyGroupDto>> CreateGroup([FromBody] CreateStudyGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Group title is required.");

        var userId = GetCurrentUserId();
        var created = await _studyGroupService.CreateGroupAsync(userId, request);
        return CreatedAtAction(nameof(GetGroups), new { id = created.Id }, created);
    }

    // POST: studygroups/{groupId}/join
    [HttpPost("{groupId:guid}/join")]
    public async Task<IActionResult> JoinGroup(Guid groupId)
    {
        var userId = GetCurrentUserId();
        var joined = await _studyGroupService.JoinGroupAsync(groupId, userId);

        if (!joined)
            return NotFound("Study group not found.");

        return Ok(new { message = "Joined study group successfully." });
    }

    // POST: studygroups/{groupId}/sessions/start
    [HttpPost("{groupId:guid}/sessions/start")]
    public async Task<ActionResult<StudySessionDto>> StartSession(
        Guid groupId,
        [FromBody] StartStudySessionRequest request)
    {
        var userId = GetCurrentUserId();
        var session = await _studyGroupService.StartSessionAsync(groupId, userId, request);
        return Ok(session);
    }

    // POST: studygroups/sessions/{sessionId}/end
    [HttpPost("sessions/{sessionId:guid}/end")]
    public async Task<IActionResult> EndSession(Guid sessionId)
    {
        var userId = GetCurrentUserId();
        var ended = await _studyGroupService.EndSessionAsync(sessionId, userId);

        if (!ended)
            return NotFound("Study session not found.");

        return Ok(new { message = "Study session ended." });
    }
}