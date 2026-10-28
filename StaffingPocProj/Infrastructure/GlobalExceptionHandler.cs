using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace StaffingPocProj.Infrastructure;

/// <summary>
/// Turns any unhandled exception into a clean ProblemDetails response.
/// The full exception is logged; details are shown to the caller only in Development.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The client closed the request. Nothing to report.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return true;

        logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        var (status, title) = exception switch
        {
            SqlException => (StatusCodes.Status503ServiceUnavailable, "The database is not available right now."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = httpContext.Request.Path,
            Detail = environment.IsDevelopment() ? exception.Message : null
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
