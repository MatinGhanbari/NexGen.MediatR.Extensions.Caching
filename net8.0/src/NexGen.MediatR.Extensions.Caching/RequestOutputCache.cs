using FluentResults;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Containers;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching;

public sealed class RequestOutputCache<TRequest, TResponse>
    : IRequestOutputCache<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<RequestOutputCache<TRequest, TResponse>> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly TimeSpan? _expirationRelativeToNow;

    public RequestOutputCache(
        ILogger<RequestOutputCache<TRequest, TResponse>> logger,
        IMemoryCache memoryCache,
        TimeSpan? expirationRelativeToNow = null)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _expirationRelativeToNow = expirationRelativeToNow ?? TimeSpan.FromSeconds(RequestCacheConstants.ExpirationInSeconds);
    }

    public async Task<Result<TResponse>> GetAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = RequestOutputCacheHelper.GetCacheKey(request);

            if (!_memoryCache.TryGetValue(cacheKey, out var response) || response == null)
                return Result.Fail(ErrorMessages.ResponseNotFound);

            _logger.LogInformation(string.Format(ErrorMessages.CacheHit, typeof(TRequest).Name));
            return Result.Ok((TResponse)response);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.Message);
            return Result.Fail(exception.Message);
        }
    }

    public async Task<Result> SetAsync(TRequest request, TResponse response, IEnumerable<string>? tags = null, int expirationInSeconds = default, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = RequestOutputCacheHelper.GetCacheKey(request);
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationInSeconds != default ? TimeSpan.FromSeconds(expirationInSeconds) : _expirationRelativeToNow,
            };

            foreach (var tag in tags)
            {
                if (RequestOutputCacheContainer.CacheTags.TryGetValue(tag, out HashSet<Type>? tagTypes))
                {
                    tagTypes ??= [];
                    tagTypes.Add(typeof(TRequest));
                }
                else
                {
                    tagTypes = [];
                    tagTypes.Add(typeof(TRequest));
                    RequestOutputCacheContainer.CacheTags.TryAdd(tag, tagTypes);
                }
            }

            if (RequestOutputCacheContainer.CacheTypes.TryGetValue(typeof(TRequest), out HashSet<string>? cacheTypes))
            {
                cacheTypes ??= [];
                cacheTypes.Add(cacheKey);
            }
            else
            {
                cacheTypes = [];
                cacheTypes.Add(cacheKey);
                RequestOutputCacheContainer.CacheTypes.TryAdd(typeof(TRequest), cacheTypes);
            }

            _memoryCache.Set(cacheKey, response, options);

            return Result.Ok();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.Message);
            return Result.Fail(exception.Message);
        }
    }

    public async Task<Result> EvictByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var tag in tags)
            {
                if (!RequestOutputCacheContainer.CacheTags.TryGetValue(tag, out HashSet<Type>? tagTypes))
                    continue;

                tagTypes ??= [];
                await EvictTypesAsync(tagTypes, cancellationToken);
            }

            return Result.Ok();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.Message);
            return Result.Fail(exception.Message);
        }
    }

    private async Task<Result> EvictTypesAsync(HashSet<Type> tagTypes, CancellationToken cancellationToken = default)
    {
        foreach (var tagType in tagTypes)
        {
            if (!RequestOutputCacheContainer.CacheTypes.TryGetValue(tagType, out HashSet<string>? cacheTypes))
                continue;

            foreach (var cacheKey in cacheTypes)
            {
                _memoryCache.Remove(cacheKey);
            }
        }

        return Result.Ok();
    }
}