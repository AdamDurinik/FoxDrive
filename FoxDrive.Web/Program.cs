using Microsoft.AspNetCore.Authentication.Cookies;
using FoxDrive.Web.Services;   
using FoxDrive.Web.Options;  
using Microsoft.EntityFrameworkCore;
using FoxDrive.Data;

var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "foxdrive_users.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddControllersWithViews();
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddSingleton<SharesService>();

// --- Connection string (config first, fallback to D:\FoxData) ---
var cfgConn = builder.Configuration.GetConnectionString("AppDb");
var conn = !string.IsNullOrWhiteSpace(cfgConn)
    ? cfgConn
    : @"Data Source=D:\FoxData\foxdrive_users.db;Mode=ReadWriteCreate;Cache=Shared";
Directory.CreateDirectory(Path.GetDirectoryName(conn.Replace("Data Source=",""))!);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(conn));
// Cookie auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/auth/login";
        opt.LogoutPath = "/auth/logout";
        opt.AccessDeniedPath = "/auth/login";
        opt.ExpireTimeSpan = TimeSpan.FromDays(14);
        opt.SlidingExpiration = true;
        opt.Cookie.Name = "foxdrive.auth";
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Allow running admin commands from CLI
if (args.Length > 0)
{
    using var scope = app.Services.CreateScope();
    
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

    var exit = AdminTools.Run(args, scope.ServiceProvider);
    Environment.ExitCode = exit;
    return; 
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
