using CampusOne.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CampusOne.Application.DTOs.Auth;
using CampusOne.Application.Interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CampusOne.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IProfilePictureStorage _pictureStorage;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IProfilePictureStorage pictureStorage)
        {
            _userManager = userManager;
            _configuration = configuration;
            _pictureStorage = pictureStorage;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var email = request.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
                return Unauthorized(new { message = "Invalid Email or Password" });

            if (await _userManager.IsLockedOutAsync(user))
                return Unauthorized(new { message = "Account is Temporarily Locked" });

            var PasswordValid = await _userManager.CheckPasswordAsync(
                user, request.Password);

            if (!PasswordValid)
            {
                await _userManager.AccessFailedAsync(user);
                return Unauthorized(new { message = "Invalid Email or Password" });
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var response = await CreateTokenResponse(user);

            return Ok(response);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return Unauthorized(new { message = "Invalid or Expired Token" });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                firstName = user.FirstName,
                lastName = user.LastName,
                fullName = user.FirstName + " " + user.LastName,
                email = user.Email,
                department = user.Department,
                academicYear = user.AcademicYear,
                profilePictureUrl = user.ProfilePictureUrl,
                roles = roles
            });
        }

        [Authorize]
        [HttpPost("upload-profile-picture")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return Unauthorized(new { message = "Invalid or Expired Token" });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please select an image." });

            const long maxFileSize = 2 * 1024 * 1024;

            if (file.Length > maxFileSize)
                return BadRequest(new { message = "Image size must not exceed 2MB." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png")
            {
                return BadRequest(new
                {
                    message = "Only JPG and PNG images are allowed."
                });
            }

            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png"
            };

            if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(new { message = "Invalid image content type." });

            await using var stream = file.OpenReadStream();

            var header = new byte[8];
            var bytesRead = await stream.ReadAsync(
                header.AsMemory(0, header.Length),
                cancellationToken);

            stream.Position = 0;

            var isJpeg = bytesRead >= 3 &&
                         header[0] == 0xFF &&
                         header[1] == 0xD8 &&
                         header[2] == 0xFF;

            var isPng = bytesRead >= 8 &&
                        header[0] == 0x89 &&
                        header[1] == 0x50 &&
                        header[2] == 0x4E &&
                        header[3] == 0x47 &&
                        header[4] == 0x0D &&
                        header[5] == 0x0A &&
                        header[6] == 0x1A &&
                        header[7] == 0x0A;

            if (!isJpeg && !isPng)
                return BadRequest(new { message = "Invalid image file." });

            if ((isJpeg && extension != ".jpg" && extension != ".jpeg") ||
                (isPng && extension != ".png"))
            {
                return BadRequest(new { message = "Image extension does not match its content." });
            }

            await _pictureStorage.SaveAsync(
                user.Id,
                stream,
                extension,
                cancellationToken);

            user.ProfilePictureUrl = $"/Auth/profile-picture/{user.Id}";

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new
            {
                message = "Profile picture uploaded successfully.",
                profilePictureUrl = user.ProfilePictureUrl
            });
        }

        [AllowAnonymous]
        [HttpGet("profile-picture/{userId}")]
        public async Task<IActionResult> GetProfilePicture(
        string userId,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null || string.IsNullOrEmpty(user.ProfilePictureUrl))
                return NotFound(new { message = "Profile picture not found." });

            var stream = await _pictureStorage.OpenReadAsync(
                user.Id,
                cancellationToken);

            if (stream is null)
                return NotFound(new { message = "Profile picture file not found." });

            var header = new byte[8];

            var bytesRead = await stream.ReadAsync(
                header.AsMemory(0, header.Length),
                cancellationToken);

            stream.Position = 0;

            var isPng = bytesRead >= 8 &&
                        header[0] == 0x89 &&
                        header[1] == 0x50 &&
                        header[2] == 0x4E &&
                        header[3] == 0x47 &&
                        header[4] == 0x0D &&
                        header[5] == 0x0A &&
                        header[6] == 0x1A &&
                        header[7] == 0x0A;

            var isJpeg = bytesRead >= 3 &&
                         header[0] == 0xFF &&
                         header[1] == 0xD8 &&
                         header[2] == 0xFF;

            if (!isPng && !isJpeg)
            {
                await stream.DisposeAsync();
                return BadRequest(new { message = "Invalid image file." });
            }

            var contentType = isPng ? "image/png" : "image/jpeg";

            return File(stream, contentType);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordRequest request)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return Unauthorized(new { message = "Invalid or Expired Token" });

            var result = await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });

            return Ok(new { message = "Password Changed Successfully" });
        }

        private async Task<AuthResponse> CreateTokenResponse(
            ApplicationUser user)
        {
            var secret = _configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("Jwt Secret is not configured");

            var issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("Jwt Issuer is not configured");

            var audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("Jwt Audience is not configured");

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email!),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName!)
            };

            claims.AddRange(
                roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddDays(7);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new AuthResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt,
                Email = user.Email!,
                Roles = roles
            };
        }
    }
}
