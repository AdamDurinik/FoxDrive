using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoxDrive.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();

        var dbPath = Path.GetFullPath(Path.Combine("..", "FoxDrive.Web", "Data", "foxdrive_users.db"));
        options.UseSqlite($"Data Source={dbPath}",
            b => b.MigrationsAssembly("FoxDrive.Data"));

        return new AppDbContext(options.Options);
    }
}
