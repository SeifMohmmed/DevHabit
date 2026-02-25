using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace DevHabit.Api.Middleware;

/// <summary>
/// Handles FluentValidation exceptions.
/// Converts validation failures into structured ProblemDetails response.
/// </summary>
public sealed class ValidationExceptionHandler(
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // If exception is not a FluentValidation ValidationException,
        // let the next handler process it.
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        // Set HTTP status code to 400 (Bad Request)
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var context = new ProblemDetailsContext()
        {
            HttpContext = httpContext,
            Exception = exception,

            // Base ProblemDetails response
            ProblemDetails = new()
            {
                Detail = "One or more validation error occured",
                Status = StatusCodes.Status400BadRequest,
            }
        };

        // Group validation errors by property name
        var errors = validationException.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                g => g.Key.ToLower(), // normalize keys
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        // Add errors dictionary into extensions section
        // This matches common API validation response formats
        context.ProblemDetails.Extensions.Add("errors", errors);

        return await problemDetailsService.TryWriteAsync(context);
    }
}
