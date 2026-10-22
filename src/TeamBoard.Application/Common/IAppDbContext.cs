using Microsoft.EntityFrameworkCore;
using TeamBoard.Domain.Boards;
using TeamBoard.Domain.Tasks;

namespace TeamBoard.Application.Common;

// Implemented by TeamBoardDbContext in Infrastructure; services depend on this, not on SQL Server.
public interface IAppDbContext
{
    DbSet<Board> Boards { get; }
    DbSet<TaskItem> Tasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
