using TeamBoard.Api.Responses;

namespace TeamBoard.Api.Setup;

public static class ErrorResponseSetup
{
    // Errors that never reach a controller still answer with the envelope:
    // unhandled exceptions (500) and responses with a status but no body (unknown route 404, wrong method 405).
    public static WebApplication UseApiErrorResponses(this WebApplication app)
    {
        // The middleware logs the exception; the client only gets a generic message and the traceId.
        app.UseExceptionHandler(errorApp => errorApp.Run(http =>
            http.Response.WriteAsJsonAsync(
                ApiResponse.Fail(http, ApiResponse.MessageFor(StatusCodes.Status500InternalServerError)))));

        // Only runs when the response has no body yet, so /health's "Unhealthy" text is left alone.
        app.UseStatusCodePages(context =>
        {
            var http = context.HttpContext;
            return http.Response.WriteAsJsonAsync(ApiResponse.Fail(http, ApiResponse.MessageFor(http.Response.StatusCode)));
        });

        return app;
    }
}
