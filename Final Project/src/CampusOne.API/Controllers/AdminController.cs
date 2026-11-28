using CampusOne.Domain.Entities.Emergency.Enums;
using CampusOne.Domain.Enums;
using CampusOne.Infrastructure.Identity;
using CampusOne.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CampusOne.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private static readonly string[] ValidRoles = ["Student", "Professor", "Staff", "Admin"];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public class CreateAdminUserRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Student";

        public string? Department { get; set; }
        public string? AcademicYear { get; set; }
    }

    public class UpdateUserRoleRequest
    {
        [Required]
        public string Role { get; set; } = string.Empty;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync(cancellationToken);

        var roleCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Student"] = 0,
            ["Professor"] = 0,
            ["Staff"] = 0,
            ["Admin"] = 0
        };

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "Student";
            if (roleCounts.ContainsKey(primaryRole))
                roleCounts[primaryRole]++;
            else
                roleCounts[primaryRole] = 1;
        }

        var activeLostAndFound = await _context.LostAndFoundItems
            .CountAsync(x => x.Status != LostAndFoundStatus.Resolved, cancellationToken);

        var resolvedLostAndFound = await _context.LostAndFoundItems
            .CountAsync(x => x.Status == LostAndFoundStatus.Resolved, cancellationToken);

        var activeEmergencies = await _context.EmergencyIncidents
            .CountAsync(x => x.Status != EmergencyStatus.Resolved, cancellationToken);

        var totalStudyGroups = await _context.StudyGroups
            .CountAsync(cancellationToken);

        return Ok(new
        {
            totalUsers = users.Count,
            roleCounts,
            activeLostAndFound,
            resolvedLostAndFound,
            activeEmergencies,
            totalStudyGroups
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        CancellationToken cancellationToken)
    {
        var query = _userManager.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(term) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.Department != null && u.Department.ToLower().Contains(term)));
        }

        var users = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);

        var result = new List<object>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "Student";

            if (!string.IsNullOrWhiteSpace(role) &&
                !string.Equals(primaryRole, role.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(new
            {
                id = user.Id,
                firstName = user.FirstName,
                lastName = user.LastName,
                fullName = $"{user.FirstName} {user.LastName}".Trim(),
                email = user.Email,
                role = primaryRole,
                department = user.Department,
                academicYear = user.AcademicYear,
                profilePictureUrl = user.ProfilePictureUrl
            });
        }

        return Ok(result);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequest request)
    {
        var normalizedRole = ValidRoles.FirstOrDefault(r =>
            string.Equals(r, request.Role.Trim(), StringComparison.OrdinalIgnoreCase));

        if (normalizedRole == null)
            return BadRequest($"Invalid role. Allowed roles: {string.Join(", ", ValidRoles)}.");

        var existing = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existing != null)
            return Conflict("A user with this email already exists.");

        if (!await _roleManager.RoleExistsAsync(normalizedRole))
            await _roleManager.CreateAsync(new IdentityRole(normalizedRole));

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Department = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim(),
            AcademicYear = string.IsNullOrWhiteSpace(request.AcademicYear) ? null : request.AcademicYear.Trim()
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
            return BadRequest(errors);
        }

        await _userManager.AddToRoleAsync(user, normalizedRole);

        return Ok(new
        {
            id = user.Id,
            firstName = user.FirstName,
            lastName = user.LastName,
            fullName = $"{user.FirstName} {user.LastName}".Trim(),
            email = user.Email,
            role = normalizedRole,
            department = user.Department,
            academicYear = user.AcademicYear
        });
    }

    [HttpPatch("users/{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(
        string userId,
        [FromBody] UpdateUserRoleRequest request)
    {
        var normalizedRole = ValidRoles.FirstOrDefault(r =>
            string.Equals(r, request.Role.Trim(), StringComparison.OrdinalIgnoreCase));

        if (normalizedRole == null)
            return BadRequest($"Invalid role. Allowed roles: {string.Join(", ", ValidRoles)}.");

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("You cannot remove your own Admin role.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        if (!await _roleManager.RoleExistsAsync(normalizedRole))
            await _roleManager.CreateAsync(new IdentityRole(normalizedRole));

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        await _userManager.AddToRoleAsync(user, normalizedRole);

        return Ok(new { id = user.Id, role = normalizedRole });
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase))
            return BadRequest("You cannot delete your own account while logged in.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return BadRequest(errors);
        }

        return NoContent();
    }
}