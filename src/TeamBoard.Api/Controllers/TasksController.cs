using Microsoft.AspNetCore.Mvc;
using TeamBoard.Api.Responses;
using TeamBoard.Application.Tasks;
using TeamBoard.Domain.Tasks;

namespace TeamBoard.Api.Controllers;

[Route("api/tasks")]
public class TasksController(TaskService tasks) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetAll(
        [FromQuery] int? boardId, [FromQuery] TaskItemStatus? status, CancellationToken ct) =>
        ApiResponse.Ok(await tasks.GetAllAsync(boardId, status, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetById(int id, CancellationToken ct) =>
        OkOrFail(await tasks.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<ApiResponse<TaskDto>>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> Create(TaskUpsertDto input, CancellationToken ct) =>
        CreatedOrFail(await tasks.CreateAsync(input, ct),
            nameof(GetById), null, t => new { id = t.Id }, "Đã tạo task.");

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, TaskUpsertDto input, CancellationToken ct) =>
        OkOrFail(await tasks.UpdateAsync(id, input, ct), "Đã cập nhật task.");

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) =>
        OkOrFail(await tasks.DeleteAsync(id, ct), "Đã xoá task.");
}
