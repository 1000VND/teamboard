using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace TeamBoard.Api.Responses;

// Replaces MVC's ProblemDetailsClientErrorFactory: status-code-only results inside controllers
// (NotFound(), 415 from the JSON input formatter, …) get the envelope body too.
public class ApiResponseClientErrorFactory : IClientErrorFactory
{
    public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
    {
        var status = clientError.StatusCode ?? StatusCodes.Status500InternalServerError;
        return new ObjectResult(ApiResponse.Fail(actionContext.HttpContext, ApiResponse.MessageFor(status)))
        {
            StatusCode = status
        };
    }
}
