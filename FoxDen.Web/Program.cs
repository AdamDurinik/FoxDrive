using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using FoxDrive.Data;

var builder = WebApplication.CreateBuilder(args);

// DBs
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Users")
        ?? "Data Source=D:\\FoxData\\foxdrive_users.db;Mode=ReadWriteCreate;Cache=Shared"));

builder.Services.AddDbContext<ShoppingDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("FoxDen")
        ?? "Data Source=D:\\FoxData\\foxden.db;Mode=ReadWriteCreate;Cache=Shared"));

builder.Services.AddControllersWithViews();

// Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/auth/login";
        opt.LogoutPath = "/auth/logout";
        opt.AccessDeniedPath = "/auth/login";
        opt.ExpireTimeSpan = TimeSpan.FromDays(14);
        opt.SlidingExpiration = true;
        opt.Cookie.Name = "foxden.auth";
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

Directory.CreateDirectory("D:\\FoxData");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/Home/Index"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "shopping",
    pattern: "shopping/{action=Index}/{id?}",
    defaults: new { controller = "Shopping" });

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ShoppingDbContext>().Database.Migrate();
}

app.Run();
