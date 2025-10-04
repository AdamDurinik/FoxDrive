using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using FoxHint.Admin.Data;
using FoxHint.Admin;
using FoxHint.Admin.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// SQLite for admin users
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlite("Data Source=Data/admin_users.db"));

builder.Services.AddSingleton<SystemInfoService>();

// Cookie auth

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/auth/login";
        opt.LogoutPath = "/auth/logout";
        opt.AccessDeniedPath = "/auth/login";
        opt.ExpireTimeSpan = TimeSpan.FromDays(7);
        opt.SlidingExpiration = true;
        opt.Cookie.Name = "foxhint.admin.auth";
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SameSite = SameSiteMode.Lax;
        //opt.Cookie.SecurePolicy = CookieSecurePolicy.Always; // force HTTPS cookies
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (args.Length > 0)
{
    var tmp = WebApplication.CreateBuilder(args);
    tmp.Services.AddDbContext<AdminDbContext>(options =>
        options.UseSqlite("Data Source=Data/admin_users.db"));

    using var sp = tmp.Services.BuildServiceProvider();
    Environment.ExitCode = AdminTools.Run(args, sp);
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
