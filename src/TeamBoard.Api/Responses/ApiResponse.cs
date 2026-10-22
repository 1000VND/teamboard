using System.Diagnostics;
using System.Text.Json.Serialization;

namespace TeamBoard.Api.Responses;

// Body of every API response (except /health): { success, data, message }.
// Failures add "errors" (validation, keyed by JSON field) and "traceId" (to find the request in the logs).
// The HTTP status code is still set correctly — success is never signalled only by this body.
public record ApiResponse<T>(bool Success, T? Data, string? Message)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; init; }
}

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string? message = null) => new(true, data, message);

    public static ApiResponse<object> Ok(string message) => new(true, null, message);

    public static ApiResponse<object> Fail(HttpContext http, string message, IDictionary<string, string[]>? errors = null) =>
        new(false, null, message)
        {
            Errors = errors,
            TraceId = Activity.Current?.Id ?? http.TraceIdentifier
        };

    // For errors raised by the framework itself (unknown route, wrong method, unhandled exception…)
    public static string MessageFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Yêu cầu không hợp lệ.",
        StatusCodes.Status401Unauthorized => "Chưa đăng nhập.",
        StatusCodes.Status403Forbidden => "Không có quyền thực hiện.",
        StatusCodes.Status404NotFound => "Không tìm thấy.",
        StatusCodes.Status405MethodNotAllowed => "Phương thức HTTP không được hỗ trợ.",
        StatusCodes.Status415UnsupportedMediaType => "Content-Type không được hỗ trợ, hãy gửi application/json.",
        >= 500 => "Đã có lỗi xảy ra, vui lòng thử lại sau.",
        _ => "Yêu cầu không thành công."
    };
}
