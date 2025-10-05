using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using FoxDrive.Data;
using FoxDrive.Data.Models;

namespace FoxDrive.Data
{
    public static class AdminTools
    {
        public static int Run(string[] args, IServiceProvider services)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  dotnet run -- adduser <username> <password> <role>");
                Console.WriteLine("  dotnet run -- deluser <username>");
                Console.WriteLine("  dotnet run -- listusers");
                return 1;
            }

            var db = services.GetRequiredService<AppDbContext>();

            switch (args[0].ToLowerInvariant())
            {
                case "adduser":
                    if (args.Length != 4) { Console.WriteLine("Usage: dotnet run -- adduser <username> <password> <role>"); return 2; }
                    var u = args[1];
                    var p = args[2];
                    var r = args[3];

                    var role = r.ToLower() switch
                    {
                        "admin" => UserRole.Admin,
                        "user" => UserRole.User,
                        _ => (UserRole)(-1)
                    };
                    if (role == (UserRole)(-1)) { Console.WriteLine("Role must be 'admin' or 'user'."); return 2; }


                    if (db.Users.Any(x => x.Username == u))
                    {
                        Console.WriteLine($"User '{u}' already exists.");
                        return 3;
                    }

                    var hasher = new PasswordHasher<string>();
                    var hash = hasher.HashPassword(u, p);

                    db.Users.Add(new User { Username = u, PasswordHash = hash, Role = role, CreatedAt = DateTime.UtcNow });
                    db.SaveChanges();
                    Console.WriteLine($"✅ {r} '{u}' added.");
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
                        Console.WriteLine($"{usr.Id} : {usr.Role} - {usr.Username}  (created {usr.CreatedAt:u})");
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

                case "setrole":
                    if (args.Length != 3) { Console.WriteLine("Usage: dotnet run -- setrole <username> <admin|user|viewer>"); return 2; }
                    {
                        var setRoleUser = db.Users.SingleOrDefault(x => x.Username == args[1]);
                        if (setRoleUser == null) { Console.WriteLine($"User '{args[1]}' not found."); return 4; }

                        var roleArg = args[2].Trim().ToLowerInvariant();

                        // If Role is an enum in your model:
                        // map text -> enum safely (adjust names if yours differ)
                        try {
                            // Try to parse enum by name ignoring case; fallback to manual map
                            // Replace "UserRole" with your actual enum type if named differently
                            setRoleUser.Role = roleArg switch
                            {
                                "admin"  => UserRole.Admin,
                                "user"   => UserRole.User,
                                "viewer" => UserRole.Viewer,
                                _ => throw new Exception("Invalid role")
                            };
                        }
                        catch {
                            Console.WriteLine("Invalid role. Use: admin | user | viewer");
                            return 5;
                        }

                        db.SaveChanges();
                        Console.WriteLine($"✅ Role for '{setRoleUser.Username}' set to {setRoleUser.Role}.");
                        return 0;
                    }

                case "approve":
                    if (args.Length != 2) { Console.WriteLine("Usage: dotnet run -- approve <username>"); return 2; }
                    {
                        var approveUser = db.Users.SingleOrDefault(x => x.Username == args[1]);
                        if (approveUser == null) { Console.WriteLine($"User '{args[1]}' not found."); return 4; }
                        approveUser.IsApproved = true;
                        db.SaveChanges();
                        Console.WriteLine($"✅ User '{approveUser.Username}' approved.");
                        return 0;
                    }

                case "setemail":
                    if (args.Length != 3) { Console.WriteLine("Usage: dotnet run -- setemail <username> <email>"); return 2; }
                    {
                        var setEmailUser = db.Users.SingleOrDefault(x => x.Username == args[1]);
                        if (setEmailUser == null) { Console.WriteLine($"User '{args[1]}' not found."); return 4; }
                        setEmailUser.Email = string.IsNullOrWhiteSpace(args[2]) ? null : args[2].Trim();
                        db.SaveChanges();
                        Console.WriteLine($"✅ Email for '{setEmailUser.Username}' set to '{setEmailUser.Email ?? "(null)"}'.");
                        return 0;
                    }

                default:
                    Console.WriteLine("Unknown command.");
                    return 1;
            }
        }
    }
}
