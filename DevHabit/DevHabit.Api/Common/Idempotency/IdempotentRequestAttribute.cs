using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace DevHabit.Api.Common.Idempotency;

/// <summary>
/// Action filter attribute that ensures idempotency for HTTP requests.
/// It prevents duplicate processing by caching the response status code
/// using a unique Idempotency-Key header.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentRequestAttribute : Attribute, IAsyncActionFilter
{
    /// <summary>
    /// Header name expected from the client to uniquely identify the request.
    /// </summary>
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    /// <summary>
    /// Default duration for how long the idempotency result should stay cached.
    /// </summary>
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Executes before and after the action method to enforce idempotency.
    /// </summary>
    /// <param name="context">Provides information about the current executing request.</param>
    /// <param name="next">Delegate to execute the next action in the pipeline.</param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Try to read Idempotency-Key header and validate it as a GUID
        if (!context.HttpContext.Request.Headers.TryGetValue(
            IdempotencyKeyHeader,
            out StringValues idempotenceKeyValue) ||
            !Guid.TryParse(idempotenceKeyValue, out Guid idempotenceKey))
        {
            // Resolve ProblemDetailsFactory to create a standardized error response
            ProblemDetailsFactory problemDetailsFactory = context.HttpContext.RequestServices
            .GetRequiredService<ProblemDetailsFactory>();

            // Create ProblemDetails object describing the error
            ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(
                httpContext: context.HttpContext,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: $"Invalid or missing {IdempotencyKeyHeader} header");

            // Short-circuit the pipeline with a 400 response
            context.Result = new BadRequestObjectResult(problemDetails);

            return;
        }

        // Resolve in-memory cache service
        IMemoryCache cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

        // Build a unique cache key using the idempotency GUID
        string cacheKey = $"idempotence:{idempotenceKey}";

        // Try to get previously cached status code
        int? statusCode = cache.Get<int?>(cacheKey);

        // If found, return cached response status without executing action again
        if (statusCode is not null)
        {
            var result = new StatusCodeResult(statusCode.Value);
            context.Result = result;
            return;
        }

        // Execute the action since this request has not been processed before
        ActionExecutedContext executedContext = await next();

        // After execution, cache the status code if result is an ObjectResult
        if (executedContext.Result is ObjectResult objectResult)
        {
            cache.Set(cacheKey, objectResult.StatusCode, DefaultCacheDuration);
        }
    }
}
