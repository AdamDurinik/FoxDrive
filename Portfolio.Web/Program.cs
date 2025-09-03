var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "contact-short",
    pattern: "Contact",
    defaults: new { controller = "Home", action = "Contact" });

app.MapControllerRoute(
    name: "contact-short",
    pattern: "Privacy",
    defaults: new { controller = "Home", action = "Privacy" });

app.MapControllerRoute(
    name: "projects-short",
    pattern: "Projects",
    defaults: new { controller = "Home", action = "Projects" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
