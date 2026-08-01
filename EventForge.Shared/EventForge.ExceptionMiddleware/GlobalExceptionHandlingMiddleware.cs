using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http; // Для HttpContext и WriteAsJsonAsync
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace EventForge.ExceptionMiddleware;

/// <summary>
/// Middleware для глобальной обработки исключений в ASP.NET Core приложении
/// </summary>
public class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger,
    IOptions<ExceptionHandlingOptions> options)
{
    private readonly ILogger _logger = logger;
    private readonly ExceptionHandlingOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleException(httpContext, ex);
        }
    }

    private async Task HandleException(HttpContext httpContext, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception. Method={Method}, Path={Path}, RequestId={RequestId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Request.Headers["x-request-id"]);

        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var (statusCode, problemDetails) = _options.ExceptionHandler(exception);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType());
    }
}
