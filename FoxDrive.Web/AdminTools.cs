using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using FoxDrive.Web.Data;

namespace FoxDrive.Web
{
    public static class AdminTools
    {
        public static int Run(string[] args, IServiceProvider services)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  dotnet run -- adduser <username> <password>");
                Console.WriteLine("  dotnet run -- deluser <username>");
                Console.WriteLine("  dotnet run -- listusers");
                return 1;
            }

            var db = services.GetRequiredService<AppDbContext>();

            switch (args[0].ToLowerInvariant())
            {
                case "adduser":
                    if (args.Length != 3) { Console.WriteLine("Usage: dotnet run -- adduser <username> <password>"); return 2; }
                    var u = args[1];
                    var p = args[2];

                    if (db.Users.Any(x => x.Username == u))
                    {
                        Console.WriteLine($"User '{u}' already exists.");
                        return 3;
                    }

                    var hasher = new PasswordHasher<string>();
                    var hash = hasher.HashPassword(u, p);

                    db.Users.Add(new User { Username = u, PasswordHash = hash, CreatedAt = DateTime.UtcNow });
                    db.SaveChanges();
                    Console.WriteLine($"✅ User '{u}' added.");
                    return 0;

                case "deluser":
                    if (args.Length != 2) { Console.WriteLine("Usage: dotnet run -- deluser <username>"); return 2; }
                    var name = args[1];
                    var entity = db.Users.SingleOrDefault(x => x.Username == name);
                    if (entity == null) { Console.WriteLine($"User '{name}' not found."); return 4; }
                    db.Users.Remove(entity);
                    db.SaveChanges();
                    Console.WriteLine($"🗑️  User '{name}' deleted.");
                    return 0;

                case "listusers":
                    foreach (var usr in db.Users.OrderBy(x => x.Username))
                        Console.WriteLine($"- {usr.Username}  (created {usr.CreatedAt:u})");
                    return 0;
                case "changepw":
                    if (args.Length != 3) { Console.WriteLine("Usage: dotnet run -- changepw <username> <newpassword>"); return 2; }
                    var user = db.Users.SingleOrDefault(x => x.Username == args[1]);
                    if (user == null) { Console.WriteLine($"User '{args[1]}' not found."); return 4; }
                    var ph = new PasswordHasher<string>();
                    user.PasswordHash = ph.HashPassword(user.Username, args[2]);
                    db.SaveChanges();
                    Console.WriteLine($"🔐 Password updated for '{user.Username}'.");
                    return 0;

                default:
                    Console.WriteLine("Unknown command.");
                    return 1;
            }
        }
    }
}
