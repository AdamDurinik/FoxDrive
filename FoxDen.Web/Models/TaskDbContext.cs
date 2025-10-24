using Microsoft.EntityFrameworkCore;

namespace FoxDen.Web.Models;
public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

    public DbSet<TaskGroup> TaskGroups => Set<TaskGroup>();
    public DbSet<TaskItem>  TaskItems  => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<TaskGroup>()
         .HasMany(g => g.Items)
         .WithOne(i => i.Group!)
         .HasForeignKey(i => i.GroupId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TaskGroup>().HasIndex(g => new { g.Done, g.CreatedUtc });
        b.Entity<TaskItem>().HasIndex(i => new { i.GroupId, i.Done, i.CreatedUtc });
    }
}


// dotnet ef migrations add AddUserAdminFields -p FoxDrive.Data -s FoxDrive.Admin -o Data/Migrations

