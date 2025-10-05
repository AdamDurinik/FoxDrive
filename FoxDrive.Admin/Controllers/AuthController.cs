using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoxDrive.Data;

namespace FoxDrive.Admin.Controllers;

[Route("auth")]
public class AuthController : Controller
{
    private readonly AppDbContext _db;
    public AuthController(AppDbContext db) => _db = db;

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = "/")
    {
        ViewData["ReturnUrl"] = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return View(); 
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPost(string username, string password, string? returnUrl = "/")
    {
        username = (username ?? "").Trim();

        // Fetch user
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
        if(user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password");
            ViewData["ReturnUrl"] = returnUrl;
            return View("Login");
        }
        if(user.Role != UserRole.Admin)
        {
            ModelState.AddModelError(string.Empty, "Access denied.");
            ViewData["ReturnUrl"] = returnUrl;
            return View("Login");
        }

        // Always run hasher to avoid revealing if user exists
        var hasher = new PasswordHasher<string>();
        var verify = user is null
            ? PasswordVerificationResult.Failed
            : hasher.VerifyHashedPassword(username, user.PasswordHash, password);

        if (verify == PasswordVerificationResult.Success ||
            verify == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Rehash if algorithm upgraded
            if (verify == PasswordVerificationResult.SuccessRehashNeeded && user is not null)
            {
                var newHash = hasher.HashPassword(username, password);
                var tracked = await _db.Users.SingleAsync(u => u.Id == user.Id);
                tracked.PasswordHash = newHash;
                await _db.SaveChangesAsync();
            }

            string roleString = user?.Role.ToString()?.ToLowerInvariant() ?? "user";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("uname", username),
                new Claim(ClaimTypes.Role, roleString) // <-- IMPORTANT
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14) });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Dashboard", "Home");
        }

        ModelState.AddModelError(string.Empty, "Invalid username or password");
        ViewData["ReturnUrl"] = returnUrl;
        return View("Login");
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
