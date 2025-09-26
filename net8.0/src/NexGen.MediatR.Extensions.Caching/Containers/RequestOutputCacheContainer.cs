using FluentResults;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using System.Collections.ObjectModel;

namespace NexGen.MediatR.Extensions.Caching.Containers;

public class RequestOutputCacheContainer : IRequestOutputCacheContainer
{
    /// <summary>
    /// Maps request types to a set of associated cache tags.
    /// Key: request type (<see cref="Type"/>)
    /// Value: set of tags (<see cref="HashSet{String}"/>)
    /// </summary>
    public Dictionary<string, HashSet<string?>> CacheTypes = [];

    /// <summary>
    /// Maps cache tags to a set of request types that use them.
    /// Key: cache tag (<see cref="string"/>)
    /// Value: set of request types (<see cref="HashSet{Type}"/>)
    /// </summary>
    public Dictionary<string, HashSet<string>> CacheTags = [];

    /// <summary>
    /// Maps request types to response types.
    /// Key: request type (<see cref="Type"/>)
    /// Value: response type (<see cref="Type"/>)
    /// </summary>
    public Dictionary<Type, Type> RequestResponseTypes = [];

    public async Task<Type?> GetResponseTypeAsync<TRequest>(CancellationToken cancellationToken = default)
    {
        RequestResponseTypes.TryGetValue(typeof(TRequest), out var type);
        return type;
    }

    public async Task<Result> UpdateContainerAsync<TRequest>(IEnumerable<string>? tags = null, string? cacheKey = null, Type responseType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var updateCacheTag = AddOrUpdateCacheTag<TRequest>(tags);
            var updateCacheType = AddOrUpdateCacheType<TRequest>(cacheKey);

            if (!updateCacheTag.IsSuccess || !updateCacheType.IsSuccess)
                return Result.Fail(ErrorMessages.ContainerUpdatesFails);

            RequestResponseTypes.TryAdd(typeof(TRequest), responseType);
            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    public async Task<ReadOnlyDictionary<string, HashSet<string>>> GetCacheTagsAsync(CancellationToken cancellationToken = default)
    {
        return CacheTags.AsReadOnly();
    }   

    public async Task<ReadOnlyDictionary<string, HashSet<string?>>> GetCacheTypesAsync(CancellationToken cancellationToken = default)
    {
        return CacheTypes.AsReadOnly();
    }

    private Result AddOrUpdateCacheTag<TRequest>(IEnumerable<string>? tags = null)
    {
        try
        {
            if (tags == null)
                return Result.Ok();

            foreach (var tag in tags)
            {
                if (CacheTags.TryGetValue(tag, out HashSet<string>? tagTypes))
                {
                    tagTypes ??= [];
                    tagTypes.Add(typeof(TRequest).FullName);
                }
                else
                {
                    tagTypes = [typeof(TRequest).FullName];
                    CacheTags.TryAdd(tag, tagTypes);
                }
            }

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    private Result AddOrUpdateCacheType<TRequest>(string? cacheKey = null)
    {
        try
        {
            if (cacheKey == null)
                return Result.Ok();

            if (CacheTypes.TryGetValue(typeof(TRequest).FullName, out HashSet<string?>? cacheTypes))
            {
                cacheTypes ??= [];
                cacheTypes.Add(cacheKey);
            }
            else
            {
                cacheTypes = [cacheKey];
                CacheTypes.TryAdd(typeof(TRequest).FullName, cacheTypes);
            }

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }
}