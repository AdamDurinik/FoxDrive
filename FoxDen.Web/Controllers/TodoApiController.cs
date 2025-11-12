using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoxDen.Web.Models; // TaskDbContext, TaskGroup, TaskItem

namespace FoxDen.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/todo")]
public class TodoApiController : ControllerBase
{
    private readonly TodoDbContext _db;
    public TodoApiController(TodoDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> GetAll()
    {
        var data = await _db.TodoGroups
            .OrderBy(g => g.Done)
            .ThenBy(g => g.CreatedUtc)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Done,
                items = g.Items
            .OrderBy(i => i.Done)
            .ThenBy(i => i.CreatedUtc)
            .Select(i => new { i.Id, i.GroupId, i.Text, i.Done })
            .ToList()
            })
            .ToListAsync();


        return Ok(data);
    }

    public record CreateGroupDto(string Name);
    public record RenameDto(string Name);
    public record CreateItemDto(int GroupId, string Text);
    public record UpdateItemDto(string? Text, int? GroupId, bool? Done);
    public record SetDoneDto(bool Done);

    // POST: /api/todo/group
    [HttpPost("group")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required");
        var g = new TodoGroup { Name = dto.Name.Trim() };
        _db.TodoGroups.Add(g);
        await _db.SaveChangesAsync();
        return Ok(new { g.Id, g.Name, g.Done, g.CreatedUtc });
    }


    // PUT: /api/todo/group/{id}/rename
    [HttpPut("group/{id:int}/rename")]
    public async Task<IActionResult> RenameGroup(int id, [FromBody] RenameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required");
        var g = await _db.TodoGroups.FindAsync(id);
        if (g == null) return NotFound();
        g.Name = dto.Name.Trim();
        await _db.SaveChangesAsync();
        return Ok(new { g.Id, g.Name, g.Done });
    }


    // Post: /api/todo/group/{id}/toggle
    [HttpPost("group/{id:int}/toggle")]
    public async Task<IActionResult> SetGroupDone(int id)
    {
        var g = await _db.TodoGroups.FindAsync(id);
        if (g == null) return NotFound();
        g.Done = !g.Done;
        await _db.SaveChangesAsync();
        return Ok(new { g.Id, g.Name, g.Done });
    }


    // DELETE: /api/todo/group/{id}
    [HttpDelete("group/{id:int}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var g = await _db.TodoGroups.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (g == null) return NotFound();
        _db.TodoItems.RemoveRange(g.Items);
        _db.TodoGroups.Remove(g);
        await _db.SaveChangesAsync();
        return NoContent();
    }


    // POST: /api/todo/item
    [HttpPost("item")]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Text)) return BadRequest("Text required");
        var groupExists = await _db.TodoGroups.AnyAsync(g => g.Id == dto.GroupId);
        if (!groupExists) return NotFound("Group not found");
        var item = new TodoItem { GroupId = dto.GroupId, Text = dto.Text.Trim(), Done = false };
        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { item.Id, item.GroupId, item.Text, item.Done, item.CreatedUtc });
    }


    // Post: /api/todo/item/{id}/toggle
    [HttpPost("item/{id:int}/toggle")]
    public async Task<IActionResult> SetItemDone(int id)
    {
        var item = await _db.TodoItems.FindAsync(id);
        if (item == null) return NotFound();
        item.Done = !item.Done;
        await _db.SaveChangesAsync();
        return Ok(new { item.Id, item.GroupId, item.Text, item.Done });
    }


    // PUT: /api/todo/item/{id}
    // Partial update: text, done, groupId
    [HttpPut("item/{id:int}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateItemDto dto)
    {
        var it = await _db.TodoItems.FirstOrDefaultAsync(x => x.Id == id);
        if (it == null) return NotFound();


        if (dto.Text is not null)
        {
            var t = dto.Text.Trim();
            if (t.Length == 0) return BadRequest("Text cannot be empty");
            it.Text = t;
        }
        if (dto.Done is not null)
        {
            it.Done = dto.Done.Value;
        }
        
        if (dto.GroupId is not null)
        {
            var exists = await _db.TodoGroups.AnyAsync(g => g.Id == dto.GroupId.Value);
            if (!exists) return NotFound("Target group not found");
            it.GroupId = dto.GroupId.Value;
        }
        await _db.SaveChangesAsync();
        return Ok(new { it.Id, it.GroupId, it.Text, it.Done, it.CreatedUtc });
    }


    // DELETE: /api/todo/item/{id}
    [HttpDelete("item/{id:int}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var it = await _db.TodoItems.FindAsync(id);
        if (it == null) return NotFound();
        _db.TodoItems.Remove(it);
        await _db.SaveChangesAsync();
        return NoContent();
    }

}