using System.Linq.Expressions;
using TeamBoard.Domain.Tasks;

namespace TeamBoard.Application.Tasks;

public static class TaskMappings
{
    // Expression form so EF Core translates it to SQL inside Select(...)
    public static readonly Expression<Func<TaskItem, TaskDto>> ToDtoExpression = t =>
        new TaskDto(t.Id, t.Title, t.Description, t.Status, t.Assignee, t.DueDate, t.CreatedAt, t.BoardId);

    private static readonly Func<TaskItem, TaskDto> ToDtoCompiled = ToDtoExpression.Compile();

    public static TaskDto ToDto(this TaskItem task) => ToDtoCompiled(task);

    public static void Apply(this TaskItem task, TaskUpsertDto input)
    {
        task.Title = input.Title;
        task.Description = input.Description;
        task.Status = input.Status;
        task.Assignee = input.Assignee;
        task.DueDate = input.DueDate;
        task.BoardId = input.BoardId;
    }
}
