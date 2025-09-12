using FluentResults;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Containers;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Helpers;
using ErrorMessages = NexGen.MediatR.Extensions.Caching.Garnet.Constants.ErrorMessages;

namespace NexGen.MediatR.Extensions.Caching.Garnet;

public sealed class GarnetRequestOutputCache<TRequest, TResponse>
    : IRequestOutputCache<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<GarnetRequestOutputCache<TRequest, TResponse>> _logger;
    private readonly IDistributedCache _cache;

    public GarnetRequestOutputCache(
        ILogger<GarnetRequestOutputCache<TRequest, TResponse>> logger,
        IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<TResponse>> GetAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = RequestOutputCacheHelper.GetCacheKey(request);

            var response = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (response == null)
                return Result.Fail(ErrorMessages.ResponseNotFound);

            _logger.LogInformation(ErrorMessages.CacheHit, typeof(TRequest).Name);
            return Result.Ok((TResponse)JsonConvert.DeserializeObject<TResponse>(response)!);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return Result.Fail(exception.Message);
        }
    }

    public async Task<Result> SetAsync(TRequest request, TResponse response, IEnumerable<string>? tags = null, int expirationInSeconds = default, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = RequestOutputCacheHelper.GetCacheKey(request);
            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = expirationInSeconds != default ? TimeSpan.FromSeconds(expirationInSeconds) : null,
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

            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response), options, cancellationToken);

            return Result.Ok();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
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
            _logger.LogError(exception, exception.Message);
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
                await _cache.RemoveAsync(cacheKey, cancellationToken);
            }
        }

        return Result.Ok();
    }
}