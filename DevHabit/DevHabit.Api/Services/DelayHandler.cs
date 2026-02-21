namespace DevHabit.Api.Services;

/// <summary>
/// Custom DelegatingHandler that introduces an artificial delay
/// before forwarding the HTTP request to the next handler in the pipeline.
/// 
/// Useful for:
/// - Testing resilience strategies (retry, timeout, hedging)
/// - Simulating slow network responses
/// </summary>
public sealed class DelayHandler : DelegatingHandler
{
    /// <summary>
    /// Overrides the SendAsync method to add a delay before sending the request.
    /// </summary>
    /// <param name="request">The outgoing HTTP request.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The HTTP response from the next handler.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Simulate network latency (10 seconds)
        await Task.Delay(10000, cancellationToken);

        // Continue processing the request pipeline
        return await base.SendAsync(request, cancellationToken);
    }
}
