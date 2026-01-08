using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoxDen.Web.Models;
using FoxDen.Web.Models.Recepie;
[Authorize]
[ApiController]
[Route("api/recepie")]
public class RecepieApiController : ControllerBase
{
    private readonly RecepieDbContext _db;
    public RecepieApiController(RecepieDbContext db) => _db = db;

    // ======================
    // OVERVIEW
    // ======================
    [HttpGet("")]
    public async Task<IEnumerable<RecepieGroup>> List()
        => await _db.RecepieGroups
            .Include(g => g.Versions)
                .ThenInclude(v => v.Photo)
            .AsNoTracking()
            .ToListAsync();

    // ======================
    // VIEW
    // ======================
    [HttpGet("version/{id:int}")]
    public async Task<IActionResult> GetVersion(int id)
    {
        var v = await _db.RecepieVersions
            .Include(x => x.Photo)
            .Include(x => x.Ingredients)
                .ThenInclude(i => i.Ingredient)
            .Include(x => x.Steps)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return v == null ? NotFound() : Ok(v);
    }

    // ======================
    // CREATE
    // ======================
    public record CreateDto(
        string Name,
        int Servings,
        IEnumerable<IngredientDto> Ingredients,
        IEnumerable<StepDto> Steps
    );

    public record IngredientDto(
        string Name,
        float Quantity,
        RecepieQuantityType QuantityType
    );

    public record StepDto(int Order, string Description);

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name required.");

        var group = new RecepieGroup { Name = dto.Name.Trim() };
        var version = new RecepieVersion
        {
            Name = "v1",
            Servings = dto.Servings > 0 ? dto.Servings : 1,
            CreatedUtc = DateTime.UtcNow
        };

        foreach (var i in dto.Ingredients ?? Enumerable.Empty<IngredientDto>())
        {
            if (string.IsNullOrWhiteSpace(i.Name)) continue;

            var ing = await _db.RecepieIngredients
                .FirstOrDefaultAsync(x => x.Name == i.Name.Trim())
                ?? new RecepieIngredient { Name = i.Name.Trim() };

            version.Ingredients.Add(new RecepieItem
            {
                Ingredient = ing,
                Quantity = i.Quantity,
                QuantityType = i.QuantityType
            });
        }

        foreach (var s in dto.Steps ?? Enumerable.Empty<StepDto>())
        {
            if (!string.IsNullOrWhiteSpace(s.Description))
                version.Steps.Add(new RecepieProcess
                {
                    Description = s.Description.Trim()
                });
        }

        group.Versions.Add(version);
        _db.RecepieGroups.Add(group);
        await _db.SaveChangesAsync();

        return Ok(new { groupId = group.Id, versionId = version.Id });
    }
}
