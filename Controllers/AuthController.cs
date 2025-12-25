using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using NavioBackend.DTOs;
using NavioBackend.Interfaces;
using NavioBackend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NavioBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _config;

        public AuthController(IUserRepository userRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _config = config;
        }

        [HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest req)
{
    try
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Identifier))
            return BadRequest(new { message = "Identifier required." });

        User user = null;
        // if (req.Identifier.Contains("@"))
        // {
        //     user = await _userRepo.GetByEmailAsync(req.Identifier);
        // }
        var identifier = req.Identifier.Trim().ToLower();

if (identifier.Contains("@"))
{
    user = await _userRepo.GetByEmailAsync(identifier);
}
        // else
        // {
        //     user = await _userRepo.GetByIdAsync(req.Identifier);
        //     if (user == null)
        //         user = await _userRepo.GetByEmailAsync(req.Identifier);
        // }
        else
{
    user = await _userRepo.GetByIdAsync(identifier);
    if (user == null)
        user = await _userRepo.GetByEmailAsync(identifier);
}

        if (user == null)
            return Unauthorized(new { message = "User not found." });

        // if (!string.IsNullOrWhiteSpace(req.Role) &&
        //     !user.Role.Equals(req.Role, StringComparison.OrdinalIgnoreCase))
        // {
        //     return Unauthorized(new { message = "Role mismatch." });
        // }

        var reqRole = req.Role?.Replace(" ", "").Trim();
var userRole = user.Role?.Replace(" ", "").Trim();

if (!string.IsNullOrWhiteSpace(reqRole) &&
    !userRole.Equals(reqRole, StringComparison.OrdinalIgnoreCase))
{
    return Unauthorized(new { message = "Role mismatch." });
}

        // Password verification supporting both BCrypt and SHA-256 hex hashes
        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            bool passwordMatches = false;

            if (user.PasswordHash.StartsWith("$2"))
            {
                try
                {
                    passwordMatches = BCrypt.Net.BCrypt.Verify(req.Password ?? string.Empty, user.PasswordHash);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Server error verifying bcrypt password.", error = ex.Message });
                }
            }
            else
            {
                // SHA-256 hex compare (matches your UserRepository.Hash implementation)
                using var sha = System.Security.Cryptography.SHA256.Create();
                var pwBytes = System.Text.Encoding.UTF8.GetBytes(req.Password ?? string.Empty);
                var hashBytes = sha.ComputeHash(pwBytes);
                var hex = System.Convert.ToHexString(hashBytes).ToLower();
                passwordMatches = string.Equals(hex, user.PasswordHash, StringComparison.OrdinalIgnoreCase);
            }

            if (!passwordMatches)
                return Unauthorized(new { message = "Incorrect password." });
        }
        else
        {
            // No stored hash — demo fallback (allow or reject based on your choice).
            // For demo: allow. For stricter behavior: return Unauthorized(...)
        }

        string token = GenerateToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Server error during login.", error = ex.Message });
    }
}

        private string GenerateToken(User user)
        {
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("userId", user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPut("change-password")]
[Authorize]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
{
    if (dto == null ||
        string.IsNullOrWhiteSpace(dto.CurrentPassword) ||
        string.IsNullOrWhiteSpace(dto.NewPassword))
    {
        return BadRequest(new { message = "Invalid payload" });
    }

    var userId = User.FindFirst("userId")?.Value;
    if (userId == null)
        return Unauthorized();

    var user = await _userRepo.GetByIdAsync(userId);
    if (user == null)
        return Unauthorized();

    // Verify current password
    bool matches = false;

    if (user.PasswordHash.StartsWith("$2"))
    {
        matches = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
    }
    else
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = Convert.ToHexString(
            sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dto.CurrentPassword))
        ).ToLower();

        matches = hash == user.PasswordHash;
    }

    if (!matches)
        return BadRequest(new { message = "Current password is incorrect" });

    // Update password (hash inside repository logic)
    //user.PasswordHash = dto.NewPassword;
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

    var updated = await _userRepo.UpdateAsync(userId, user);
    if (!updated)
        return StatusCode(500, new { message = "Failed to update password" });

    return Ok(new { message = "Password changed successfully" });
}

    }
}
