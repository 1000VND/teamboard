using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TeamBoard.Api.Responses;
using TeamBoard.Application.Boards;
using TeamBoard.Application.Tasks;

namespace TeamBoard.Api.Tests;

// The self-assessment checks from the week-0 brief, plus the error cases around them.
// Every body (except /health) is the { success, data, message } envelope, with the HTTP status still correct.
public class BoardsAndTasksTests(TeamBoardApiFactory factory) : IClassFixture<TeamBoardApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Board_with_two_tasks_returns_exactly_two_tasks()
    {
        var board = await CreateBoardAsync("Sprint 1");
        await CreateTaskInBoardAsync(board.Id, "Thiết kế DB");
        await CreateTaskInBoardAsync(board.Id, "Viết API");

        var response = await _client.GetAsync($"/api/boards/{board.Id}/tasks");
        var body = await ReadAsync<List<TaskDto>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.Success);
        Assert.Equal(["Thiết kế DB", "Viết API"], body.Data!.Select(t => t.Title));
        Assert.All(body.Data!, t => Assert.Equal(board.Id, t.BoardId));
    }

    [Fact]
    public async Task Post_task_with_board_in_body_returns_201_with_location_and_message()
    {
        var board = await CreateBoardAsync("Body board");

        var response = await _client.PostAsJsonAsync("/api/tasks",
            new { title = "Từ body", status = "InProgress", boardId = board.Id });
        var body = await ReadAsync<TaskDto>(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(body.Success);
        Assert.Equal("Đã tạo task.", body.Message);
        Assert.Equal($"/api/tasks/{body.Data!.Id}", response.Headers.Location?.AbsolutePath);
    }

    [Theory]
    [InlineData("/api/boards/999999", "Board 999999 không tồn tại.")]
    [InlineData("/api/boards/999999/tasks", "Board 999999 không tồn tại.")]
    [InlineData("/api/tasks/999999", "Task 999999 không tồn tại.")]
    public async Task Get_unknown_id_returns_404_envelope_with_message(string url, string message)
    {
        var response = await _client.GetAsync(url);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("data").ValueKind);
        Assert.Equal(message, json.GetProperty("message").GetString());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Successful_update_and_delete_return_200_with_message()
    {
        var board = await CreateBoardAsync("Sẽ xoá");

        var update = await _client.PutAsJsonAsync($"/api/boards/{board.Id}", new { name = "Đã đổi tên" });
        var delete = await _client.DeleteAsync($"/api/boards/{board.Id}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Đã cập nhật board.", (await ReadAsync<object>(update)).Message);
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.True((await ReadAsync<object>(delete)).Success);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/boards/{board.Id}")).StatusCode);
    }

    [Fact]
    public async Task Update_and_delete_unknown_ids_return_404()
    {
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.PutAsJsonAsync("/api/boards/999999", new { name = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/api/boards/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/api/tasks/999999")).StatusCode);
    }

    [Fact]
    public async Task Dates_are_returned_as_utc_with_z_suffix()
    {
        var board = await CreateBoardAsync("UTC");
        await _client.PostAsJsonAsync($"/api/boards/{board.Id}/tasks",
            new { title = "Hạn chót", dueDate = "2026-10-25T17:00:00+07:00" });

        // Read back from the database, not the POST response
        var data = (await _client.GetFromJsonAsync<JsonElement>($"/api/boards/{board.Id}")).GetProperty("data");
        var task = data.GetProperty("tasks")[0];

        Assert.EndsWith("Z", data.GetProperty("createdAt").GetString());
        Assert.EndsWith("Z", task.GetProperty("createdAt").GetString());
        Assert.Equal("2026-10-25T10:00:00Z", task.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Post_response_dates_are_utc_too()
    {
        var board = await CreateBoardAsync("UTC post");

        var response = await _client.PostAsJsonAsync($"/api/boards/{board.Id}/tasks",
            new { title = "x", dueDate = "2026-10-25T17:00:00+07:00" });
        var task = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        Assert.Equal("2026-10-25T10:00:00Z", task.GetProperty("dueDate").GetString());
        Assert.EndsWith("Z", task.GetProperty("createdAt").GetString());
    }

    [Fact]
    public async Task Create_task_without_title_returns_400()
    {
        var board = await CreateBoardAsync("Validation");

        var viaBoard = await _client.PostAsJsonAsync($"/api/boards/{board.Id}/tasks", new { status = "Todo" });
        var viaTasks = await _client.PostAsJsonAsync("/api/tasks", new { boardId = board.Id });

        Assert.Equal(HttpStatusCode.BadRequest, viaBoard.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, viaTasks.StatusCode);
    }

    [Fact]
    public async Task Validation_errors_use_camel_case_keys_and_vietnamese_messages()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new { boardId = 0, assignee = new string('a', 101) });
        var body = await ReadAsync<object>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(body.Success);
        Assert.Equal("Dữ liệu không hợp lệ.", body.Message);
        Assert.Equal(["Không được để trống."], body.Errors!["title"]);
        Assert.Equal(["Tối đa 100 ký tự."], body.Errors["assignee"]);
        Assert.Equal(["Phải lớn hơn hoặc bằng 1."], body.Errors["boardId"]);
    }

    [Fact]
    public async Task Unreadable_json_reports_only_the_bad_field()
    {
        var response = await PostRawAsync("/api/tasks", """{ "title": "x", "boardId": 1, "status": "Bogus" }""");
        var body = await ReadAsync<object>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["$.status"], body.Errors!.Keys);
        Assert.Equal(["Giá trị không hợp lệ."], body.Errors["$.status"]);
    }

    [Fact]
    public async Task Missing_body_reports_one_error_at_json_root()
    {
        var response = await PostRawAsync("/api/tasks", "");
        var body = await ReadAsync<object>(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["$"], body.Errors!.Keys);
        Assert.Equal(["Thiếu body của request."], body.Errors["$"]);
    }

    [Fact]
    public async Task Create_task_for_missing_board_returns_404_in_route_and_400_in_body()
    {
        var viaBoard = await _client.PostAsJsonAsync("/api/boards/999999/tasks", new { title = "x" });
        var viaTasks = await _client.PostAsJsonAsync("/api/tasks", new { title = "x", boardId = 999999 });

        Assert.Equal(HttpStatusCode.NotFound, viaBoard.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, viaTasks.StatusCode);
        Assert.Equal("Board 999999 không tồn tại.", (await ReadAsync<object>(viaTasks)).Message);
    }

    [Theory]
    [InlineData("GET", "/api/khong-co", HttpStatusCode.NotFound, "Không tìm thấy.")]
    [InlineData("PATCH", "/api/boards/1", HttpStatusCode.MethodNotAllowed, "Phương thức HTTP không được hỗ trợ.")]
    public async Task Errors_outside_controllers_use_the_envelope_too(
        string method, string url, HttpStatusCode status, string message)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));
        var body = await ReadAsync<object>(response);

        Assert.Equal(status, response.StatusCode);
        Assert.False(body.Success);
        Assert.Equal(message, body.Message);
    }

    [Fact]
    public async Task Health_is_plain_text_not_an_envelope()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    private static async Task<ApiResponse<T>> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ApiResponse<T>>(TeamBoardApiFactory.Json))!;

    private Task<HttpResponseMessage> PostRawAsync(string url, string json) =>
        _client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

    private async Task<BoardDto> CreateBoardAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/boards", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync<BoardDto>(response)).Data!;
    }

    private async Task CreateTaskInBoardAsync(int boardId, string title)
    {
        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks", new { title });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
