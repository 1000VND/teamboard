using Microsoft.AspNetCore.Mvc;
using TeamBoard.Api.Responses;
using TeamBoard.Application.Common;

namespace TeamBoard.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    // Application errors → envelope with success: false and the matching status: NotFound → 404, Validation → 400.
    protected ObjectResult Fail(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, null)
        };
        return StatusCode(status, ApiResponse.Fail(HttpContext, error.Message));
    }

    protected ActionResult<ApiResponse<T>> OkOrFail<T>(Result<T> result, string? message = null) =>
        result.IsSuccess ? ApiResponse.Ok(result.Value!, message) : Fail(result.Error!);

    protected ActionResult<ApiResponse<object>> OkOrFail(Result result, string message) =>
        result.IsSuccess ? ApiResponse.Ok(message) : Fail(result.Error!);

    protected ActionResult<ApiResponse<T>> CreatedOrFail<T>(Result<T> result, string actionName, string? controllerName,
        Func<T, object> routeValues, string message) =>
        result.IsSuccess
            ? CreatedAtAction(actionName, controllerName, routeValues(result.Value!), ApiResponse.Ok(result.Value!, message))
            : Fail(result.Error!);
}
