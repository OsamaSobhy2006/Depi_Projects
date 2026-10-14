using CampusOne.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CampusOne.Application.DTOs.Auth;
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

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var email = request.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);
            if(user is null) 
                return Unauthorized(new { message = "Invalid Email or Password" });

            if(await _userManager.IsLockedOutAsync(user)) 
                return Unauthorized(new { message = "Account is Temporarily Locked" });

            var PasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!PasswordValid)
            {
                await _userManager.AccessFailedAsync(user);
                return Unauthorized(new {message = "Invalid Email or Password" });
            }
            await _userManager.ResetAccessFailedCountAsync(user);
            var response = await CreateTokenResponse(user);

            return Ok(response);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Unauthorized(new { message = "Invalid or Expired Token" });

            var result = await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new {errors = result.Errors.Select(e => e.Description) });

            return Ok(new {message = "Password Changed Successfully" });
        }

        private async Task<AuthResponse> CreateTokenResponse(ApplicationUser user)
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

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
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
