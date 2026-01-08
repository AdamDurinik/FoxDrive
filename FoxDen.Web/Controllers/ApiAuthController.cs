using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FoxDrive.Data; // adjust if your DbContext namespace is different
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FoxDen.Web.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class ApiAuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;

        public ApiAuthController(AppDbContext db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
        }

        public class LoginResponse
        {
            public string Token { get; set; } = "";
            public DateTime Expires { get; set; }
            public string? Role { get; set; }
        }

        [HttpPost("token")]
        public async Task<IActionResult> Token([FromBody] LoginRequest req)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "username and password required" });

            var username = req.Username.Trim();
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);

            var hasher = new PasswordHasher<string>();
            var verify = user is null
                ? PasswordVerificationResult.Failed
                : hasher.VerifyHashedPassword(username, user.PasswordHash, req.Password);

            if (verify != PasswordVerificationResult.Success && verify != PasswordVerificationResult.SuccessRehashNeeded)
            {
                return Unauthorized(new { error = "Invalid credentials" });
            }

            // rehash if needed (keeps parity with your MVC login)
            if (verify == PasswordVerificationResult.SuccessRehashNeeded && user is not null)
            {
                var newHash = hasher.HashPassword(username, req.Password);
                var tracked = await _db.Users.SingleAsync(u => u.Id == user.Id);
                tracked.PasswordHash = newHash;
                await _db.SaveChangesAsync();
            }

            var roleString = user?.Role.ToString()?.ToLowerInvariant() ?? "user";

            // read JWT config (make sure you added it to appsettings.json)
            var key = _cfg["Jwt:Key"];
            var issuer = _cfg["Jwt:Issuer"] ?? "foxden";
            var audience = _cfg["Jwt:Audience"] ?? "foxden";

            if (string.IsNullOrEmpty(key) || key.Length < 16)
                return StatusCode(500, new { error = "JWT secret is not configured correctly on server" });

            var expires = DateTime.UtcNow.AddDays(14);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim("uname", username),
                new Claim(ClaimTypes.Role, roleString),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var resp = new LoginResponse
            {
                Token = tokenString,
                Expires = expires,
                Role = roleString
            };

            return Ok(resp);
        }
    }
}
