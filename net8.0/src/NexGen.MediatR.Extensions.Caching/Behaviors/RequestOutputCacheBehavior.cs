using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically caches the response of requests
/// marked with the <see cref="RequestOutputCacheAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the MediatR request.</typeparam>
/// <typeparam name="TResponse">The type of the MediatR response.</typeparam>
public class RequestOutputCacheBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IRequestOutputCache<TRequest, TResponse> _requestOutputCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="requestOutputCache">
    /// The cache service that handles storing and retrieving request responses.
    /// </param>
    public RequestOutputCacheBehavior(IRequestOutputCache<TRequest, TResponse> requestOutputCache)
    {
        _requestOutputCache = requestOutputCache;
    }

    /// <summary>
    /// Handles a request by checking if a cached response exists. 
    /// If a cached response is found, it is returned immediately. 
    /// Otherwise, the request is processed and the response is cached.
    /// </summary>
    /// <param name="request">The MediatR request being handled.</param>
    /// <param name="next">The next delegate in the MediatR pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response either from cache or from the handler.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_requestOutputCache == null)
            return await next(cancellationToken);

        var attribute = (RequestOutputCacheAttribute)typeof(TRequest)
                        .GetCustomAttributes(typeof(RequestOutputCacheAttribute), false)
                        .FirstOrDefault()!;
        if (attribute == null)
            return await next(cancellationToken);

        var cachedResult = await _requestOutputCache.GetAsync(request, cancellationToken);
        if (cachedResult.IsSuccess)
            return cachedResult.Value;

        var result = await next(cancellationToken);

        var tags = attribute.Tags;
        var expiration = attribute.ExpirationInSeconds;
        await _requestOutputCache.SetAsync(request, result, tags, expiration, cancellationToken).ConfigureAwait(false);
        return result;
    }
}