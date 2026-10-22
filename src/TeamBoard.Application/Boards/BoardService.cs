using Microsoft.EntityFrameworkCore;
using TeamBoard.Application.Common;
using TeamBoard.Application.Tasks;
using TeamBoard.Domain.Boards;

namespace TeamBoard.Application.Boards;

public class BoardService(IAppDbContext db)
{
    public Task<List<BoardDto>> GetAllAsync(CancellationToken ct = default) =>
        db.Boards
            .OrderBy(b => b.Id)
            .Select(b => new BoardDto(b.Id, b.Name, b.Description, b.CreatedAt, b.Tasks.Count))
            .ToListAsync(ct);

    public async Task<Result<BoardDetailDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var board = await db.Boards
            .Where(b => b.Id == id)
            .Select(b => new BoardDetailDto(b.Id, b.Name, b.Description, b.CreatedAt,
                b.Tasks.AsQueryable().OrderBy(t => t.Id).Select(TaskMappings.ToDtoExpression).ToList()))
            .FirstOrDefaultAsync(ct);

        return board is null ? BoardNotFound(id) : board;
    }

    public async Task<BoardDto> CreateAsync(BoardUpsertDto input, CancellationToken ct = default)
    {
        var board = new Board { Name = input.Name, Description = input.Description };
        db.Boards.Add(board);
        await db.SaveChangesAsync(ct);

        return new BoardDto(board.Id, board.Name, board.Description, board.CreatedAt, 0);
    }

    public async Task<Result> UpdateAsync(int id, BoardUpsertDto input, CancellationToken ct = default)
    {
        var board = await db.Boards.FindAsync([id], ct);
        if (board is null) return BoardNotFound(id);

        board.Name = input.Name;
        board.Description = input.Description;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var board = await db.Boards.FindAsync([id], ct);
        if (board is null) return BoardNotFound(id);

        db.Boards.Remove(board); // tasks are cascade-deleted by the database (see README)
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static Error BoardNotFound(int id) => Error.NotFound($"Board {id} không tồn tại.");
}
