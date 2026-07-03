using System.Text.Json;

namespace R_FACTORY_BE.Common;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteErrorResponse(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponse(context, StatusCodes.Status500InternalServerError, "Internal server error", ex);
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string message, Exception? ex)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new Dictionary<string, object?> { { "message", message } };
        if (ex != null && System.Diagnostics.Debugger.IsAttached)
        {
            response["detail"] = ex.Message;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        }));
    }
}
