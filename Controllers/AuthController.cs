using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FileSystem_Honeywell.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FileSystem_Honeywell.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly FSDBContext _db;

        public AuthController(IConfiguration config, FSDBContext db)
        {
            _config = config;
            _db = db;
        }

        public record LoginRequest(string Username, string Password);

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthUser request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required.");

            var exists = await _db.Users
                .AnyAsync(u => u.Username.ToLower() == request.Username.ToLower(), cancellationToken);

            if (exists)
                return Conflict("Username already exists.");

            var user = new AuthUser
            {
                Username = request.Username,
                Password = HashPassword(request.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            var token = GenerateJwtToken(user.Username);
            return Ok(new { token });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required.");

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower(), cancellationToken);

            if (user == null)
                return Unauthorized("Invalid username or password.");

            if (!VerifyPassword(request.Password, user.Password))
                return Unauthorized("Invalid username or password.");

            var token = GenerateJwtToken(user.Username);

            return Ok(new { token });
        }


        private string GenerateJwtToken(string username)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Name, username)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == storedHash;
        }
    }
}
