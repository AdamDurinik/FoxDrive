using Microsoft.EntityFrameworkCore;
using FoxDrive.Data.Models;

namespace FoxDrive.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }
    public DbSet<User> Users => Set<User>();
}
