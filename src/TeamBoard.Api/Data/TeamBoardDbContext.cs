using Microsoft.EntityFrameworkCore;
using TeamBoard.Api.Models;

namespace TeamBoard.Api.Data;

public class TeamBoardDbContext(DbContextOptions<TeamBoardDbContext> options) : DbContext(options)
{
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Board>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<TaskItem>(t =>
        {
            t.ToTable("Tasks");
            t.Property(x => x.Title).HasMaxLength(200).IsRequired();
            t.Property(x => x.Description).HasMaxLength(1000);
            t.Property(x => x.Assignee).HasMaxLength(100);
            t.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            t.HasOne(x => x.Board)
                .WithMany(b => b.Tasks)
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
