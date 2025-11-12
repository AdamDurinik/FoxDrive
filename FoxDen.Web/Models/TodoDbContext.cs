using Microsoft.EntityFrameworkCore;

namespace FoxDen.Web.Models;
public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    public DbSet<TodoGroup> TodoGroups => Set<TodoGroup>();
    public DbSet<TodoItem>  TodoItems  => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<TodoGroup>()
         .HasMany(g => g.Items)
         .WithOne(i => i.Group!)
         .HasForeignKey(i => i.GroupId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TodoGroup>().HasIndex(g => new { g.Done, g.CreatedUtc });
        b.Entity<TodoItem>().HasIndex(i => new { i.GroupId, i.Done, i.CreatedUtc });
    }
}


// dotnet ef migrations add AddUserAdminFields -p FoxDrive.Data -s FoxDrive.Admin -o Data/Migrations

