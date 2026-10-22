using Microsoft.AspNetCore.Mvc;
using TeamBoard.Api.Responses;
using TeamBoard.Application.Boards;
using TeamBoard.Application.Tasks;

namespace TeamBoard.Api.Controllers;

[Route("api/boards")]
public class BoardsController(BoardService boards, TaskService tasks) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BoardDto>>>> GetAll(CancellationToken ct) =>
        ApiResponse.Ok(await boards.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<BoardDetailDto>>> GetById(int id, CancellationToken ct) =>
        OkOrFail(await boards.GetByIdAsync(id, ct));

    [HttpGet("{id:int}/tasks")]
    public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetTasks(int id, CancellationToken ct) =>
        OkOrFail(await tasks.GetByBoardAsync(id, ct));

    [HttpPost("{id:int}/tasks")]
    [ProducesResponseType<ApiResponse<TaskDto>>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask(int id, BoardTaskCreateDto input, CancellationToken ct) =>
        CreatedOrFail(await tasks.CreateInBoardAsync(id, input, ct),
            nameof(TasksController.GetById), "Tasks", t => new { id = t.Id }, "Đã tạo task.");

    [HttpPost]
    [ProducesResponseType<ApiResponse<BoardDto>>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<BoardDto>>> Create(BoardUpsertDto input, CancellationToken ct)
    {
        var board = await boards.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = board.Id }, ApiResponse.Ok(board, "Đã tạo board."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, BoardUpsertDto input, CancellationToken ct) =>
        OkOrFail(await boards.UpdateAsync(id, input, ct), "Đã cập nhật board.");

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) =>
        OkOrFail(await boards.DeleteAsync(id, ct), "Đã xoá board và các task của nó.");
}
