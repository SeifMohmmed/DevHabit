using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Middleware;

/// <summary>
/// Handles unhandled exceptions globally.
/// Returns standardized RFC 7807 ProblemDetails response.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle any unhandled exception.
    /// This acts as a fallback exception handler.
    /// </summary>
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // We don't expose internal exception details to the client
        // to avoid leaking sensitive information.
        return problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,

            // Standard RFC 7807 response
            ProblemDetails = new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occured while processing your request. Please try again"
                // Status defaults to 500 automatically
            }
        });
    }
}
