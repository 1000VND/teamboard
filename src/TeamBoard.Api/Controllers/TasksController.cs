using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamBoard.Api.Data;
using TeamBoard.Api.Dtos;
using TeamBoard.Api.Models;

namespace TeamBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController(TeamBoardDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetAll([FromQuery] int? boardId, [FromQuery] TaskItemStatus? status)
    {
        var query = db.Tasks.AsQueryable();
        if (boardId is not null) query = query.Where(t => t.BoardId == boardId);
        if (status is not null) query = query.Where(t => t.Status == status);

        return await query
            .OrderBy(t => t.Id)
            .Select(t => new TaskDto(t.Id, t.Title, t.Description, t.Status, t.Assignee, t.DueDate, t.CreatedAt, t.BoardId))
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskDto>> GetById(int id)
    {
        var task = await db.Tasks.FindAsync(id);
        return task is null ? NotFound() : ToDto(task);
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(TaskUpsertDto input)
    {
        if (!await db.Boards.AnyAsync(b => b.Id == input.BoardId))
            return BadRequest($"Board {input.BoardId} không tồn tại.");

        var task = new TaskItem();
        Apply(task, input);
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, ToDto(task));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TaskUpsertDto input)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return NotFound();
        if (!await db.Boards.AnyAsync(b => b.Id == input.BoardId))
            return BadRequest($"Board {input.BoardId} không tồn tại.");

        Apply(task, input);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(TaskItem task, TaskUpsertDto input)
    {
        task.Title = input.Title;
        task.Description = input.Description;
        task.Status = input.Status;
        task.Assignee = input.Assignee;
        task.DueDate = input.DueDate;
        task.BoardId = input.BoardId;
    }

    private static TaskDto ToDto(TaskItem t) =>
        new(t.Id, t.Title, t.Description, t.Status, t.Assignee, t.DueDate, t.CreatedAt, t.BoardId);
}
