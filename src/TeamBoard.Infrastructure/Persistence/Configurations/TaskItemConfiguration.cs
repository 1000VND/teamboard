using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamBoard.Domain.Tasks;

namespace TeamBoard.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> t)
    {
        t.ToTable("Tasks");
        t.Property(x => x.Title).HasMaxLength(200).IsRequired();
        t.Property(x => x.Description).HasMaxLength(1000);
        t.Property(x => x.Assignee).HasMaxLength(100);
        t.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        // Deleting a board deletes its tasks — reason in README
        t.HasOne(x => x.Board)
            .WithMany(b => b.Tasks)
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
