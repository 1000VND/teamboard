using System.ComponentModel.DataAnnotations;
using TeamBoard.Domain.Tasks;

namespace TeamBoard.Application.Tasks;

public record TaskDto(
    int Id, string Title, string? Description, TaskItemStatus Status,
    string? Assignee, DateTime? DueDate, DateTime CreatedAt, int BoardId);

public record TaskUpsertDto(
    [Required, StringLength(200)] string Title,
    [StringLength(1000)] string? Description,
    TaskItemStatus Status,
    [StringLength(100)] string? Assignee,
    DateTime? DueDate,
    [Range(1, int.MaxValue)] int BoardId);

// Body for POST /api/boards/{id}/tasks: the board comes from the route.
public record BoardTaskCreateDto(
    [Required, StringLength(200)] string Title,
    [StringLength(1000)] string? Description,
    TaskItemStatus Status,
    [StringLength(100)] string? Assignee,
    DateTime? DueDate);
