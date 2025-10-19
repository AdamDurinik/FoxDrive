using Microsoft.EntityFrameworkCore;
using FoxDen.Web.Models;

public class ShoppingDbContext : DbContext
{
    public ShoppingDbContext(DbContextOptions<ShoppingDbContext> options) : base(options) { }

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<ShoppingItem>().Property(p => p.Name).IsRequired();
        b.Entity<ShoppingItem>().HasIndex(p => new { p.Bought, p.Date });
    }
}
