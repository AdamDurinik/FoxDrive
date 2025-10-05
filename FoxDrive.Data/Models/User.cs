namespace FoxDrive.Data.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? Email { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsApproved { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginUtc { get; set; }
}
