using Microsoft.EntityFrameworkCore;
using TeamBoard.Application.Common;
using TeamBoard.Domain.Boards;
using TeamBoard.Domain.Tasks;
using TeamBoard.Infrastructure.Persistence.Converters;

namespace TeamBoard.Infrastructure.Persistence;

public class TeamBoardDbContext(DbContextOptions<TeamBoardDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    // Picks up every IEntityTypeConfiguration in Persistence/Configurations
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeamBoardDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }
}
