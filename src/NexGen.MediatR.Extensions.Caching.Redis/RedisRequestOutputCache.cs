using FluentResults;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching.Redis;

/// <summary>
/// Implementation of <see cref="IRequestOutputCache{TRequest, TResponse}"/> using Redis distributed cache.
/// </summary>
/// <typeparam name="TRequest">The type of the MediatR request.</typeparam>
/// <typeparam name="TResponse">The type of the response. Must be a class.</typeparam>
public sealed class RedisRequestOutputCache<TRequest, TResponse>
    : IRequestOutputCache<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<RedisRequestOutputCache<TRequest, TResponse>> _logger;
    private readonly IDistributedCache _cache;
    private readonly IRequestOutputCacheContainer _cacheContainer;

    /// <summary>
    /// Initializes a new instance of <see cref="RedisRequestOutputCache{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="logger">The logger used to log cache hits and errors.</param>
    /// <param name="cache">The distributed cache used for storing responses.</param>
    /// <param name="cacheContainer">The cache container</param>
    public RedisRequestOutputCache(
        ILogger<RedisRequestOutputCache<TRequest, TResponse>> logger,
        IDistributedCache cache,
        IRequestOutputCacheContainer cacheContainer)
    {
        _logger = logger;
        _cache = cache;
        _cacheContainer = cacheContainer;
    }

    /// <inheritdoc />
    public async Task<Result<TResponse>> GetAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = RequestOutputCacheHelper.GetCacheKey(request);

            var response = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (response == null)
                return Result.Fail(ErrorMessages.ResponseNotFound);

            _logger.LogInformation(ErrorMessages.CacheHit, typeof(TRequest).Name);

            var type = await _cacheContainer.GetResponseTypeAsync<TRequest>(cancellationToken);
            return Result.Ok((TResponse)JsonConvert.DeserializeObject(response, type ?? typeof(TResponse))!);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return Result.Fail(exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> SetAsync(TRequest request, TResponse response, IEnumerable<string>? tags = null, int expirationInSeconds = default, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = RequestOutputCacheHelper.GetCacheKey(request);
            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = expirationInSeconds != default ? TimeSpan.FromSeconds(expirationInSeconds) : null,
            };

            var containerResult = await _cacheContainer.UpdateContainerAsync<TRequest>(
                tags,
                cacheKey,
                response?.GetType() ?? typeof(TResponse),
                cancellationToken);
            if (containerResult.IsFailed)
                return Result.Fail(ErrorMessages.FailedToUpdateContainer);

            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response), options, cancellationToken);

            return Result.Ok();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return Result.Fail(exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> EvictByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheTags = await _cacheContainer.GetCacheTagsAsync(cancellationToken).ConfigureAwait(false);

            foreach (var tag in tags)
            {
                if (!cacheTags.TryGetValue(tag, out HashSet<string>? tagTypes))
                    continue;

                tagTypes ??= [];
                await EvictTypesAsync(tagTypes, cancellationToken).ConfigureAwait(false);
            }

            return Result.Ok();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return Result.Fail(exception.Message);
        }
    }

    public async Task<Result> FlushAll(CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheTags = await _cacheContainer.GetCacheTagsAsync(cancellationToken);

            var allTypes = cacheTags.Values
                .Where(v => v != null)
                .SelectMany(v => v!)
                .ToHashSet();

            if (!allTypes.Any())
                return Result.Ok();

            await EvictTypesAsync(allTypes, cancellationToken);

            return Result.Ok();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return Result.Fail(exception.Message);
        }
    }

    /// <summary>
    /// Removes all cache entries for the specified request types.
    /// </summary>
    /// <param name="tagTypes">The request types to evict.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A successful <see cref="Result"/> when eviction completes.</returns>
    private async Task<Result> EvictTypesAsync(HashSet<string> tagTypes, CancellationToken cancellationToken = default)
    {
        var cacheTypes = await _cacheContainer.GetCacheTypesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var tagType in tagTypes)
        {
            if (!cacheTypes.TryGetValue(tagType, out HashSet<string?>? types) || types is null)
                continue;

            foreach (var cacheType in types)
            {
                if (cacheType is not null)
                    await _cache.RemoveAsync(cacheType, cancellationToken);
            }
        }

        return Result.Ok();
    }
}