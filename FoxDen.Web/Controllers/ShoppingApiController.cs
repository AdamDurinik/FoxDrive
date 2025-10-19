using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoxDen.Web.Models;

[Authorize]
[ApiController]
[Route("api/shopping")]
public class ShoppingApiController : ControllerBase
{
    private readonly ShoppingDbContext _db;
    public ShoppingApiController(ShoppingDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IEnumerable<ShoppingItem>> List()
        => await _db.ShoppingItems.OrderBy(i => i.Bought).ThenBy(i => i.Date).ThenBy(i => i.Name).ToListAsync();

    public record UpsertDto(int? Id, string Name, string? Amount, DateTime? Date, string? Shop, bool Bought);

    [HttpPost("upsert")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upsert([FromBody] UpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required.");

        ShoppingItem entity;
        if (dto.Id is { } id && id > 0)
        {
            entity = await _db.ShoppingItems.FindAsync(id) ?? new ShoppingItem();
            if (entity.Id == 0) _db.ShoppingItems.Add(entity);
        }
        else
        {
            entity = new ShoppingItem();
            _db.ShoppingItems.Add(entity);
        }

        entity.Name   = dto.Name.Trim();
        entity.Amount = string.IsNullOrWhiteSpace(dto.Amount) ? null : dto.Amount.Trim();
        entity.Date   = dto.Date;
        entity.Shop   = string.IsNullOrWhiteSpace(dto.Shop) ? null : dto.Shop.Trim();
        entity.Bought = dto.Bought;

        await _db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpPost("{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBought(int id)
    {
        var it = await _db.ShoppingItems.FindAsync(id);
        if (it == null) return NotFound();
        it.Bought = !it.Bought;
        await _db.SaveChangesAsync();
        return Ok(new { it.Id, it.Bought });
    }

    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var it = await _db.ShoppingItems.FindAsync(id);
        if (it == null) return NotFound();
        _db.ShoppingItems.Remove(it);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("delete-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAll()
    {
        _db.ShoppingItems.RemoveRange(_db.ShoppingItems);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    
    [HttpPost("delete-all-bought")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAllBought()
    {
        var boughtItems = _db.ShoppingItems.Where(i => i.Bought);
        _db.ShoppingItems.RemoveRange(boughtItems);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
