using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FoxDrive.Data;
using FoxDen.Web.Models;
using FoxDen.Web.Models.Recepie;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// DBs
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Users")
        ?? "Data Source=D:\\FoxData\\foxdrive_users.db;Mode=ReadWriteCreate;Cache=Shared"));

builder.Services.AddDbContext<ShoppingDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("FoxDen")
        ?? "Data Source=D:\\FoxData\\foxden.db;Mode=ReadWriteCreate;Cache=Shared"));

builder.Services.AddDbContext<TaskDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("FoxDen")
        ?? "Data Source=D:\\FoxData\\foxden.db;Mode=ReadWriteCreate;Cache=Shared"));

builder.Services.AddDbContext<TodoDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("FoxDen")
        ?? "Data Source=D:\\FoxData\\foxden.db;Mode=ReadWriteCreate;Cache=Shared"));
        
builder.Services.AddDbContext<RecepieDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("FoxDen")
        ?? "Data Source=D:\\FoxData\\foxden.db;Mode=ReadWriteCreate;Cache=Shared"));
builder.Services.AddControllersWithViews();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// ===== JWT + Cookie authentication configuration =====

// Read JWT settings from configuration (appsettings.json or environment)
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "foxden";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "foxden";

// Optional: CORS for development / mobile testing
builder.Services.AddCors(cors =>
{
    cors.AddPolicy("AllowAllDev", pb =>
    {
        pb.AllowAnyHeader()
          .AllowAnyMethod()
          .AllowAnyOrigin(); // tighten in production
    });
});

// Add Authentication: keep Cookie (existing flow) + add JwtBearer
builder.Services.AddAuthentication(options =>
{
    // Keep cookies as default for MVC flows
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
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
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;

    // If jwtKey is null or too short, TokenValidationParameters will still be set but startup will fail later.
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = string.IsNullOrEmpty(jwtKey)
            ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes("placeholder-key-should-be-replaced"))
            : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(cors =>
{
    cors.AddPolicy("AllowFoxden", pb =>
    {
        pb.WithOrigins("https://foxden.foxhint.com")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials(); // if you ever use cookies
    });
});

var app = builder.Build();
app.UseCors("AllowFoxden");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

Directory.CreateDirectory("D:\\FoxData");

app.UseStaticFiles();
app.UseRouting();

// enable CORS in middleware (for dev policy)
app.UseCors("AllowAllDev");
// Trust forwarded headers from Cloudflare tunnel / proxy
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    // If you want, restrict KnownProxies or KnownNetworks to Cloudflared IPs
    // KnownNetworks = { ... }, KnownProxies = { ... }
});

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
