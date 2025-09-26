using FluentResults;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Containers;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching;

/// <summary>
/// Default implementation of <see cref="IRequestOutputCache{TRequest, TResponse}"/> using in-memory caching.
/// </summary>
/// <typeparam name="TRequest">The type of the MediatR request.</typeparam>
/// <typeparam name="TResponse">The type of the response. Must be a class.</typeparam>
public sealed class RequestOutputCache<TRequest, TResponse>
    : IRequestOutputCache<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<RequestOutputCache<TRequest, TResponse>> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IRequestOutputCacheContainer _cacheContainer;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestOutputCache{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="logger">The logger to log cache hits and errors.</param>
    /// <param name="memoryCache">The memory cache used for storing responses.</param>
    /// <param name="cacheContainer">The cache container</param>
    public RequestOutputCache(
        ILogger<RequestOutputCache<TRequest, TResponse>> logger,
        IMemoryCache memoryCache,
        IRequestOutputCacheContainer cacheContainer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _cacheContainer = cacheContainer ?? throw new ArgumentNullException(nameof(cacheContainer));
    }

    /// <inheritdoc />
    public async Task<Result<TResponse>> GetAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = RequestOutputCacheHelper.GetCacheKey(request);

            if (!_memoryCache.TryGetValue(cacheKey, out var response) || response == null)
                return Result.Fail(ErrorMessages.ResponseNotFound);

            _logger.LogInformation(ErrorMessages.CacheHit, typeof(TRequest).Name);

            return Result.Ok((TResponse)response);
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
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationInSeconds != default ? TimeSpan.FromSeconds(expirationInSeconds) : null
            };

            var containerResult = await _cacheContainer.UpdateContainerAsync<TRequest>(tags, cacheKey, response?.GetType() ?? typeof(TResponse), cancellationToken);
            if (containerResult.IsFailed)
                return Result.Fail(ErrorMessages.FailedToUpdateContainer);

            _memoryCache.Set(cacheKey, response, options);

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
            foreach (var tag in tags)
            {
                if (!_cacheContainer.GetCacheTagsAsync(cancellationToken).Result.TryGetValue(tag, out HashSet<string>? tagTypes))
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

    /// <summary>
    /// Removes all cache entries for the given request types.
    /// </summary>
    /// <param name="tagTypes">The request types whose cache entries should be evicted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A successful <see cref="Result"/> when eviction completes.</returns>
    private async Task<Result> EvictTypesAsync(HashSet<string> tagTypes, CancellationToken cancellationToken = default)
    {
        foreach (var tagType in tagTypes.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
        {
            if (!_cacheContainer.GetCacheTypesAsync(cancellationToken).Result.TryGetValue(tagType, out HashSet<string>? cacheTypes))
                continue;

            foreach (var cacheType in cacheTypes)
            {
                _memoryCache.Remove(cacheType);
            }
        }

        return Result.Ok();
    }
}