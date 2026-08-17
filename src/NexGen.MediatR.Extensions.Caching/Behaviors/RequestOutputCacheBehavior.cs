using MediatR;
using Microsoft.AspNetCore.Http;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
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
    private readonly RequestOutputCacheDefaults _defaults;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="requestOutputCache">
    /// The cache service that handles storing and retrieving request responses.
    /// </param>
    /// <param name="defaults">
    /// Optional library defaults. When <see cref="RequestOutputCacheDefaults.DefaultExpirationInSeconds"/> is set,
    /// it replaces the attribute constructor default expiration.
    /// </param>
    /// <param name="httpContextAccessor">
    /// Optional HTTP context accessor used to set the cache-hit response header
    /// when <see cref="RequestOutputCacheDefaults.EnableCacheHitResponseHeader"/> is enabled.
    /// </param>
    public RequestOutputCacheBehavior(
        IRequestOutputCache<TRequest, TResponse> requestOutputCache,
        RequestOutputCacheDefaults? defaults = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _requestOutputCache = requestOutputCache;
        _defaults = defaults ?? new RequestOutputCacheDefaults();
        _httpContextAccessor = httpContextAccessor;
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
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        if (_requestOutputCache == null)
            return await next(cancellationToken).ConfigureAwait(false);

        var attribute = (RequestOutputCacheAttribute)typeof(TRequest)
            .GetCustomAttributes(typeof(RequestOutputCacheAttribute), false)
            .FirstOrDefault()!;

        if (attribute == null)
            return await next(cancellationToken).ConfigureAwait(false);

        var cachedResult = await _requestOutputCache.GetAsync(request, cancellationToken).ConfigureAwait(false);
        if (cachedResult.IsSuccess)
        {
            TrySetCacheHitResponseHeader();
            return cachedResult.Value;
        }

        var result = await next(cancellationToken).ConfigureAwait(false);

        var tags = attribute.Tags;
        var expiration = ResolveExpiration(attribute.ExpirationInSeconds);
        await _requestOutputCache.SetAsync(request, result, tags, expiration, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private void TrySetCacheHitResponseHeader()
    {
        if (!_defaults.EnableCacheHitResponseHeader)
            return;

        var response = _httpContextAccessor?.HttpContext?.Response;
        if (response is null || response.HasStarted)
            return;

        response.Headers[RequestCacheConstants.CacheHitResponseHeaderName] =
            RequestCacheConstants.CacheHitResponseHeaderValue;
    }

    private int ResolveExpiration(int attributeExpirationInSeconds)
    {
        if (attributeExpirationInSeconds == RequestCacheConstants.DefaultExpirationInSeconds
            && _defaults.DefaultExpirationInSeconds is int providerDefault)
        {
            return providerDefault;
        }

        return attributeExpirationInSeconds;
    }
}
