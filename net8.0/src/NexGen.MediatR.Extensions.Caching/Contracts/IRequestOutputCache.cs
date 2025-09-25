using FluentResults;
using MediatR;

namespace NexGen.MediatR.Extensions.Caching.Contracts;

/// <summary>
/// Defines a contract for caching the output of MediatR requests.
/// </summary>
/// <typeparam name="TRequest">The type of the request. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response. Must be a class type.</typeparam>
public interface IRequestOutputCache<in TRequest, TResponse>
    : IRequestOutputCacheInvalidator where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Retrieves a cached response for the specified request.
    /// </summary>
    /// <param name="request">The request whose cached response should be retrieved.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="Result{TResponse}"/> containing the cached response if available.</returns>
    Task<Result<TResponse>> GetAsync(TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches the response of a request.
    /// </summary>
    /// <param name="request">The request associated with the response.</param>
    /// <param name="response">The response to cache.</param>
    /// <param name="tags">Optional tags to associate with the cache entry for grouping and invalidation.</param>
    /// <param name="expirationInSeconds">
    /// Cache lifetime in seconds. If <c>0</c>, the cache entry never expires. Defaults to library-defined expiration.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> SetAsync(TRequest request, TResponse response, IEnumerable<string>? tags = null, int expirationInSeconds = default, CancellationToken cancellationToken = default);
}