using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamBoard.Api.Data;
using TeamBoard.Api.Dtos;
using TeamBoard.Api.Models;

namespace TeamBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoardsController(TeamBoardDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BoardDto>>> GetAll()
    {
        return await db.Boards
            .OrderBy(b => b.Id)
            .Select(b => new BoardDto(b.Id, b.Name, b.Description, b.CreatedAt, b.Tasks.Count))
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BoardDetailDto>> GetById(int id)
    {
        var board = await db.Boards
            .Where(b => b.Id == id)
            .Select(b => new BoardDetailDto(b.Id, b.Name, b.Description, b.CreatedAt,
                b.Tasks.OrderBy(t => t.Id)
                    .Select(t => new TaskDto(t.Id, t.Title, t.Description, t.Status, t.Assignee, t.DueDate, t.CreatedAt, t.BoardId))
                    .ToList()))
            .FirstOrDefaultAsync();

        return board is null ? NotFound() : board;
    }

    [HttpGet("{id:int}/tasks")]
    public async Task<ActionResult<List<TaskDto>>> GetTasks(int id)
    {
        if (!await db.Boards.AnyAsync(b => b.Id == id)) return NotFound();

        return await db.Tasks
            .Where(t => t.BoardId == id)
            .OrderBy(t => t.Id)
            .Select(t => new TaskDto(t.Id, t.Title, t.Description, t.Status, t.Assignee, t.DueDate, t.CreatedAt, t.BoardId))
            .ToListAsync();
    }

    [HttpPost("{id:int}/tasks")]
    public async Task<ActionResult<TaskDto>> CreateTask(int id, BoardTaskCreateDto input)
    {
        if (!await db.Boards.AnyAsync(b => b.Id == id)) return NotFound();

        var task = new TaskItem
        {
            BoardId = id,
            Title = input.Title,
            Description = input.Description,
            Status = input.Status,
            Assignee = input.Assignee,
            DueDate = input.DueDate
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var dto = new TaskDto(task.Id, task.Title, task.Description, task.Status, task.Assignee, task.DueDate, task.CreatedAt, task.BoardId);
        return CreatedAtAction(nameof(TasksController.GetById), "Tasks", new { id = task.Id }, dto);
    }

    [HttpPost]
    public async Task<ActionResult<BoardDto>> Create(BoardUpsertDto input)
    {
        var board = new Board { Name = input.Name, Description = input.Description };
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        var dto = new BoardDto(board.Id, board.Name, board.Description, board.CreatedAt, 0);
        return CreatedAtAction(nameof(GetById), new { id = board.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, BoardUpsertDto input)
    {
        var board = await db.Boards.FindAsync(id);
        if (board is null) return NotFound();

        board.Name = input.Name;
        board.Description = input.Description;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var board = await db.Boards.FindAsync(id);
        if (board is null) return NotFound();

        db.Boards.Remove(board); // tasks are cascade-deleted
        await db.SaveChangesAsync();
        return NoContent();
    }
}
