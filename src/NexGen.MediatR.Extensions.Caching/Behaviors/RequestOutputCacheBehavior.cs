using MediatR;
using Microsoft.AspNetCore.Http;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Helpers;

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
    private readonly RequestOutputCacheConditions? _conditions;

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
    /// <param name="conditions">
    /// Optional per-request predicates registered with
    /// <see cref="RequestOutputCacheConfigurationOption.CacheWhen{TRequest, TResponse}(Func{TResponse, bool})"/>.
    /// </param>
    public RequestOutputCacheBehavior(
        IRequestOutputCache<TRequest, TResponse> requestOutputCache,
        RequestOutputCacheDefaults? defaults = null,
        IHttpContextAccessor? httpContextAccessor = null,
        RequestOutputCacheConditions? conditions = null)
    {
        _requestOutputCache = requestOutputCache;
        _defaults = defaults ?? new RequestOutputCacheDefaults();
        _httpContextAccessor = httpContextAccessor;
        _conditions = conditions;
    }

    /// <summary>
    /// Handles a request by checking if a cached response exists.
    /// If a cached response is found, it is returned immediately.
    /// Otherwise, the request is processed and the response is cached when it meets
    /// the configured cache condition.
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

        if (!ShouldCache(request, result))
            return result;

        var tags = attribute.Tags;
        var expiration = ResolveExpiration(attribute.ExpirationInSeconds);
        await _requestOutputCache.SetAsync(request, result, tags, expiration, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private bool ShouldCache(TRequest request, TResponse result)
    {
        var predicate = _conditions?.Find<TRequest, TResponse>();
        if (predicate is not null)
            return result is not null && predicate(request, result);

        return RequestOutputCacheResponseEvaluator.ShouldCache(result, _defaults.CacheUnsuccessfulResponses);
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
