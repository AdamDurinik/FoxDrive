using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using FoxDrive.Data;
using System.Text.Json.Serialization;

//[Authorize(Roles = "admin")]
[Route("admin/users")] 
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var users = await _db.Users.OrderBy(u => u.Username).ToListAsync();
        return View(users);
    }

    [HttpGet("/api/admin/users")]
    public async Task<IActionResult> ListApi()
    {
        var users = await _db.Users
            .OrderBy(u => u.Username)
            .Select(u => new {
                id = u.Id,
                username = u.Username,
                email = u.Email,
                role = u.Role.ToString().ToLowerInvariant(), // enum -> string
                isApproved = u.IsApproved,
                createdAt = u.CreatedAt,
                lastLoginUtc = u.LastLoginUtc
            })
            .ToListAsync();

        return Ok(users);
    }

    public record ResetPwDto(string newPassword);

    [HttpPost("/api/admin/users/{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveApi(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.IsApproved = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("/api/admin/users/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteApi(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        _db.Users.Remove(u);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("/api/admin/users/{id:int}/resetpw")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordApi(int id, [FromBody] ResetPwDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.newPassword) || dto.newPassword.Length < 8)
            return BadRequest("Password too short.");

        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();

        var hasher = new PasswordHasher<string>();
        u.PasswordHash = hasher.HashPassword(u.Username, dto.newPassword);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Optional: update email/role via API (your enum UserRole)
    public record UpdateUserDto(string? email, string role, bool? isApproved);

    [HttpPost("/api/admin/users/{id:int}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateApi(int id, [FromBody] UpdateUserDto dto)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();

        UserRole parsed = dto.role?.ToLowerInvariant() switch
        {
            "admin"  => UserRole.Admin,
            "user"   => UserRole.User,
            "viewer" => UserRole.Viewer,
            _ => (UserRole)(-1)
        };
        if ((int)parsed < 0) return BadRequest("Invalid role.");

        u.Email = string.IsNullOrWhiteSpace(dto.email) ? null : dto.email.Trim();
        u.Role = parsed;
        if (dto.isApproved.HasValue) u.IsApproved = dto.isApproved.Value;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
