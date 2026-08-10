using FluentResults;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Garnet.Constants;
using System.Collections.ObjectModel;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Containers;

public class GarnetOutputCacheContainer : IRequestOutputCacheContainer
{
    private readonly IDistributedCache _cache;

    public GarnetOutputCacheContainer(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<Type?> GetResponseTypeAsync<TRequest>(CancellationToken cancellationToken = default)
    {
        var response = await _cache.GetStringAsync(CacheKeys.RequestResponseTypesKey, cancellationToken);
        if (response == null) return null;

        var requestResponseTypes = (Dictionary<Type, Type>)JsonConvert.DeserializeObject(response, typeof(Dictionary<Type, Type>))!;

        return requestResponseTypes.GetValueOrDefault(typeof(TRequest));
    }

    public async Task<Result> UpdateContainerAsync<TRequest>(IEnumerable<string>? tags = null, string? cacheKey = null, Type responseType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var updateCacheTag = await AddOrUpdateCacheTag<TRequest>(tags, cancellationToken);
            var updateCacheType = await AddOrUpdateCacheType<TRequest>(cacheKey, cancellationToken);

            if (!updateCacheTag.IsSuccess || !updateCacheType.IsSuccess)
                return Result.Fail(ErrorMessages.ContainerUpdatesFails);

            var response = await _cache.GetStringAsync(CacheKeys.RequestResponseTypesKey, cancellationToken);
            var requestResponseTypes = response == null ? [] : (Dictionary<Type, Type>)JsonConvert.DeserializeObject(response, typeof(Dictionary<Type, Type>))!;
            requestResponseTypes.TryAdd(typeof(TRequest), responseType);
            await _cache.SetStringAsync(CacheKeys.RequestResponseTypesKey, JsonConvert.SerializeObject(requestResponseTypes), cancellationToken);

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    public async Task<ReadOnlyDictionary<string, HashSet<string>>> GetCacheTagsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _cache.GetStringAsync(CacheKeys.CacheTagsKey, cancellationToken);
        if (response == null) return new Dictionary<string, HashSet<string>>().AsReadOnly();

        var cacheTags = (Dictionary<string, HashSet<string>>)JsonConvert.DeserializeObject(response, typeof(Dictionary<string, HashSet<string>>))!;
        return cacheTags.AsReadOnly();
    }

    public async Task<ReadOnlyDictionary<string, HashSet<string?>>> GetCacheTypesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _cache.GetStringAsync(CacheKeys.CacheTypesKey, cancellationToken);
        if (response == null) return new Dictionary<string, HashSet<string?>>().AsReadOnly();

        var cacheTypes = (Dictionary<string, HashSet<string?>>)JsonConvert.DeserializeObject(response, typeof(Dictionary<string, HashSet<string?>>))!;
        return cacheTypes.AsReadOnly();
    }

    private async Task<Result> AddOrUpdateCacheTag<TRequest>(IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (tags == null)
                return Result.Ok();

            foreach (var tag in tags)
            {
                if (GetCacheTagsAsync(cancellationToken).Result.TryGetValue(tag, out HashSet<string>? tagTypes))
                {
                    tagTypes ??= [];
                    tagTypes.Add(typeof(TRequest).FullName);
                }
                else
                {
                    tagTypes = [typeof(TRequest).FullName];

                    var response = await _cache.GetStringAsync(CacheKeys.CacheTagsKey, cancellationToken);
                    var cacheTags = response == null ? [] : (Dictionary<string, HashSet<string>>)JsonConvert.DeserializeObject(response, typeof(Dictionary<string, HashSet<string>>))!;
                    cacheTags.TryAdd(tag, tagTypes);

                    await _cache.SetStringAsync(CacheKeys.CacheTagsKey, JsonConvert.SerializeObject(cacheTags), cancellationToken);
                }
            }

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    private async Task<Result> AddOrUpdateCacheType<TRequest>(string? cacheKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (cacheKey == null)
                return Result.Ok();

            if (GetCacheTypesAsync(cancellationToken).Result.TryGetValue(typeof(TRequest).FullName, out HashSet<string?>? types))
            {
                types ??= [];
                types.Add(cacheKey);
            }
            else
            {
                types = [cacheKey];

                var response = await _cache.GetStringAsync(CacheKeys.CacheTypesKey, cancellationToken);
                var cacheTypes = response == null ? new Dictionary<string, HashSet<string>>() : (Dictionary<string, HashSet<string?>>)JsonConvert.DeserializeObject(response, typeof(Dictionary<string, HashSet<string?>>))!;
                cacheTypes.TryAdd(typeof(TRequest).FullName, types);

                await _cache.SetStringAsync(CacheKeys.CacheTypesKey, JsonConvert.SerializeObject(cacheTypes), cancellationToken);
            }

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }
}