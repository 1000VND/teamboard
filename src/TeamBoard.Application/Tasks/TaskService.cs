using Microsoft.EntityFrameworkCore;
using TeamBoard.Application.Common;
using TeamBoard.Domain.Tasks;

namespace TeamBoard.Application.Tasks;

public class TaskService(IAppDbContext db)
{
    public Task<List<TaskDto>> GetAllAsync(int? boardId, TaskItemStatus? status, CancellationToken ct = default)
    {
        var query = db.Tasks.AsQueryable();
        if (boardId is not null) query = query.Where(t => t.BoardId == boardId);
        if (status is not null) query = query.Where(t => t.Status == status);

        return query.OrderBy(t => t.Id).Select(TaskMappings.ToDtoExpression).ToListAsync(ct);
    }

    public async Task<Result<TaskDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var task = await db.Tasks.FindAsync([id], ct);
        return task is null ? TaskNotFound(id) : task.ToDto();
    }

    public async Task<Result<List<TaskDto>>> GetByBoardAsync(int boardId, CancellationToken ct = default)
    {
        if (!await BoardExistsAsync(boardId, ct)) return Error.NotFound(BoardMissing(boardId));

        return await db.Tasks
            .Where(t => t.BoardId == boardId)
            .OrderBy(t => t.Id)
            .Select(TaskMappings.ToDtoExpression)
            .ToListAsync(ct);
    }

    // POST /api/tasks: boardId is in the body, so a missing board is a bad request (400).
    public async Task<Result<TaskDto>> CreateAsync(TaskUpsertDto input, CancellationToken ct = default)
    {
        if (!await BoardExistsAsync(input.BoardId, ct)) return Error.Validation(BoardMissing(input.BoardId));

        var task = new TaskItem();
        task.Apply(input);
        return await AddAsync(task, ct);
    }

    // POST /api/boards/{id}/tasks: boardId is in the route, so a missing board is 404.
    public async Task<Result<TaskDto>> CreateInBoardAsync(int boardId, BoardTaskCreateDto input, CancellationToken ct = default)
    {
        if (!await BoardExistsAsync(boardId, ct)) return Error.NotFound(BoardMissing(boardId));

        var task = new TaskItem
        {
            BoardId = boardId,
            Title = input.Title,
            Description = input.Description,
            Status = input.Status,
            Assignee = input.Assignee,
            DueDate = input.DueDate
        };
        return await AddAsync(task, ct);
    }

    public async Task<Result> UpdateAsync(int id, TaskUpsertDto input, CancellationToken ct = default)
    {
        var task = await db.Tasks.FindAsync([id], ct);
        if (task is null) return TaskNotFound(id);
        if (!await BoardExistsAsync(input.BoardId, ct)) return Error.Validation(BoardMissing(input.BoardId));

        task.Apply(input);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var task = await db.Tasks.FindAsync([id], ct);
        if (task is null) return TaskNotFound(id);

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<TaskDto> AddAsync(TaskItem task, CancellationToken ct)
    {
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return task.ToDto();
    }

    private Task<bool> BoardExistsAsync(int boardId, CancellationToken ct) =>
        db.Boards.AnyAsync(b => b.Id == boardId, ct);

    private static string BoardMissing(int boardId) => $"Board {boardId} không tồn tại.";

    private static Error TaskNotFound(int id) => Error.NotFound($"Task {id} không tồn tại.");
}
