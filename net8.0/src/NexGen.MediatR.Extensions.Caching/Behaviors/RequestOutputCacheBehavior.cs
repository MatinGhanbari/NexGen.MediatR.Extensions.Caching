using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.Behaviors;

public class RequestOutputCacheBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IRequestOutputCache<TRequest, TResponse> _requestOutputCache;

    public RequestOutputCacheBehavior(IRequestOutputCache<TRequest, TResponse> requestOutputCache)
    {
        _requestOutputCache = requestOutputCache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var isCacheIsNull = _requestOutputCache == null;
        if (isCacheIsNull) return await next();

        var attribute = (RequestOutputCacheAttribute)typeof(TRequest).GetCustomAttributes(typeof(RequestOutputCacheAttribute), inherit: true).FirstOrDefault()!;
        if (attribute == null) return await next();

        var cachedResult = await _requestOutputCache.GetAsync(request, cancellationToken);

        if (cachedResult.IsSuccess)
            return cachedResult.Value;

        var result = await next();

        var tags = attribute.Tags;
        var expiration = attribute.ExpirationInSeconds;
        await _requestOutputCache.SetAsync(request, result, tags, expiration, cancellationToken);
        return result;
    }
}