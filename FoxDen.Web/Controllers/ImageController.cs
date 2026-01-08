using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoxDen.Web.Models;

[AllowAnonymous]
[Route("images")]
public class ImageController : Controller
{
    private readonly IWebHostEnvironment _env;
    public ImageController(IWebHostEnvironment env) => _env = env;

    [HttpGet("{*path}")]
    public IActionResult Get(string path)
    {
        var full = Path.Combine(_env.ContentRootPath, "data/images", path);
        if (!System.IO.File.Exists(full)) return NotFound();
        return PhysicalFile(full, "image/jpeg");
    }
}
