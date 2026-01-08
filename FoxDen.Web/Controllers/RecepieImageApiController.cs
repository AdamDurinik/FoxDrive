using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoxDen.Web.Models;
using FoxDen.Web.Models.Recepie;
[Authorize]
[ApiController]
[Route("api/recepie/image")]
public class RecepieImageApiController : ControllerBase
{
    private readonly RecepieDbContext _db;
    private readonly IWebHostEnvironment _env;

    public RecepieImageApiController(
        RecepieDbContext db,
        IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost("version/{versionId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadVersionImage(
        int versionId,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No image.");

        var version = await _db.RecepieVersions.FindAsync(versionId);
        if (version == null) return NotFound();

        var relPath = $"recepies/versions/{versionId}/main.jpg";
        var absPath = Path.Combine(
            _env.ContentRootPath,
            "data/images",
            relPath);

        Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);

        await using (var fs = System.IO.File.Create(absPath))
            await file.CopyToAsync(fs);

        var img = new RecepieImage { Url = relPath };
        _db.RecepieImages.Add(img);

        version.Photo = img;
        await _db.SaveChangesAsync();

        return Ok(img);
    }
}
