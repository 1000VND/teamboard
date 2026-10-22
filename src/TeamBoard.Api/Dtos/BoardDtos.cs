using System.ComponentModel.DataAnnotations;

namespace TeamBoard.Api.Dtos;

public record BoardDto(int Id, string Name, string? Description, DateTime CreatedAt, int TaskCount);

public record BoardDetailDto(int Id, string Name, string? Description, DateTime CreatedAt, List<TaskDto> Tasks);

public record BoardUpsertDto(
    [Required, StringLength(100)] string Name,
    [StringLength(500)] string? Description);
