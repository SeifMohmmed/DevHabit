using Microsoft.Extensions.Http.Resilience;

namespace DevHabit.Api.Extensions;

/// <summary>
/// Extension methods for modifying HttpClient resilience pipeline.
/// </summary>
public static class ResilienceHttpClientBuilderExtensions
{
    /// <summary>
    /// Removes all default resilience handlers (like retry, timeout, hedging)
    /// added by Microsoft so a fully custom pipeline can be configured.
    /// </summary>
    /// <param name="builder">The IHttpClientBuilder instance.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHttpClientBuilder InternalRemoveAllResilienceHandlers(this IHttpClientBuilder builder)
    {
        builder.ConfigureAdditionalHttpMessageHandlers(
            delegate (IList<DelegatingHandler> handlers, IServiceProvider _)
            {
                // Iterate backwards to safely remove items
                for (int num = handlers.Count - 1; num >= 0; num--)
                {
                    // Remove only Microsoft resilience handlers
                    if (handlers[num] is ResilienceHandler)
                    {
                        handlers.RemoveAt(num);
                    }
                }
            });

        return builder;
    }
}
