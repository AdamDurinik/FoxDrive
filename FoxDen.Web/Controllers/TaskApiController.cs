using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoxDen.Web.Models; // TaskDbContext, TaskGroup, TaskItem

namespace FoxDen.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/task")]
public class TaskApiController : ControllerBase
{
    private readonly TaskDbContext _db;
    public TaskApiController(TaskDbContext db) => _db = db;

    // ---- READ: groups with items
    [HttpGet("")]
    public async Task<IActionResult> GetAll()
    {
        var data = await _db.TaskGroups
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

    // ---- GROUPS
    public record CreateGroupDto(string Name);

    [HttpPost("group")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required.");
        var g = new TaskGroup { Name = dto.Name.Trim() };
        _db.TaskGroups.Add(g);
        await _db.SaveChangesAsync();
        return Ok(g);
    }

    [HttpPost("group/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleGroup(int id)
    {
        var g = await _db.TaskGroups.FindAsync(id);
        if (g is null) return NotFound();
        g.Done = !g.Done;
        await _db.SaveChangesAsync();
        return Ok(new { g.Id, g.Done });
    }

    [HttpDelete("group/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var g = await _db.TaskGroups.FindAsync(id);
        if (g is null) return NotFound();
        _db.TaskGroups.Remove(g); // cascade deletes items
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- TASKS
    public record CreateTaskDto(int GroupId, string Text);

    [HttpPost("task")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Text)) return BadRequest("Text required.");
        var exists = await _db.TaskGroups.AnyAsync(g => g.Id == dto.GroupId);
        if (!exists) return BadRequest("Group not found.");

        var it = new TaskItem { GroupId = dto.GroupId, Text = dto.Text.Trim() };
        _db.TaskItems.Add(it);
        await _db.SaveChangesAsync();
        return Ok(it);
    }

    [HttpPost("task/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTask(int id)
    {
        var it = await _db.TaskItems.FindAsync(id);
        if (it is null) return NotFound();
        it.Done = !it.Done;
        await _db.SaveChangesAsync();
        return Ok(new { it.Id, it.Done });
    }

    [HttpDelete("task/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var it = await _db.TaskItems.FindAsync(id);
        if (it is null) return NotFound();
        _db.TaskItems.Remove(it);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
