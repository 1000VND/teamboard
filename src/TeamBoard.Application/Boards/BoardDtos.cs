using System.ComponentModel.DataAnnotations;
using TeamBoard.Application.Tasks;

namespace TeamBoard.Application.Boards;

public record BoardDto(int Id, string Name, string? Description, DateTime CreatedAt, int TaskCount);

public record BoardDetailDto(int Id, string Name, string? Description, DateTime CreatedAt, List<TaskDto> Tasks);

public record BoardUpsertDto(
    [Required, StringLength(100)] string Name,
    [StringLength(500)] string? Description);
