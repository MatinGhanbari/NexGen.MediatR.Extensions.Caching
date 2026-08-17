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
        var response = await _cache.GetStringAsync(CacheKeys.RequestResponseTypesKey, cancellationToken).ConfigureAwait(false);
        if (response == null) return null;

        var requestResponseTypes = DeserializeStringMap(response);
        var requestTypeName = typeof(TRequest).FullName ?? typeof(TRequest).Name;

        if (!TryResolveResponseTypeName(requestResponseTypes, requestTypeName, out var responseTypeName)
            || string.IsNullOrEmpty(responseTypeName))
        {
            return null;
        }

        return Type.GetType(responseTypeName);
    }

    public async Task<Result> UpdateContainerAsync<TRequest>(IEnumerable<string>? tags = null, string? cacheKey = null, Type? responseType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var updateCacheTag = await AddOrUpdateCacheTag<TRequest>(tags, cancellationToken).ConfigureAwait(false);
            var updateCacheType = await AddOrUpdateCacheType<TRequest>(cacheKey, cancellationToken).ConfigureAwait(false);

            if (!updateCacheTag.IsSuccess || !updateCacheType.IsSuccess)
                return Result.Fail(ErrorMessages.ContainerUpdatesFails);

            if (responseType is not null)
            {
                var response = await _cache.GetStringAsync(CacheKeys.RequestResponseTypesKey, cancellationToken).ConfigureAwait(false);
                var requestResponseTypes = DeserializeStringMap(response);
                var requestTypeName = typeof(TRequest).FullName ?? typeof(TRequest).Name;
                requestResponseTypes[requestTypeName] = responseType.AssemblyQualifiedName ?? responseType.FullName ?? responseType.Name;
                await _cache.SetStringAsync(
                    CacheKeys.RequestResponseTypesKey,
                    JsonConvert.SerializeObject(requestResponseTypes),
                    cancellationToken).ConfigureAwait(false);
            }

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    public async Task<ReadOnlyDictionary<string, HashSet<string>>> GetCacheTagsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _cache.GetStringAsync(CacheKeys.CacheTagsKey, cancellationToken).ConfigureAwait(false);
        if (response == null) return new Dictionary<string, HashSet<string>>().AsReadOnly();

        var cacheTags = (Dictionary<string, HashSet<string>>)JsonConvert.DeserializeObject(response, typeof(Dictionary<string, HashSet<string>>))!;
        return cacheTags.AsReadOnly();
    }

    public async Task<ReadOnlyDictionary<string, HashSet<string?>>> GetCacheTypesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _cache.GetStringAsync(CacheKeys.CacheTypesKey, cancellationToken).ConfigureAwait(false);
        if (response == null) return new Dictionary<string, HashSet<string?>>().AsReadOnly();

        var cacheTypes = (Dictionary<string, HashSet<string?>>)JsonConvert.DeserializeObject(response, typeof(Dictionary<string, HashSet<string?>>))!;
        return cacheTypes.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveRequestTypesAsync(
        IEnumerable<string> requestTypeNames,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestTypes = requestTypeNames.ToHashSet(StringComparer.Ordinal);
            if (requestTypes.Count == 0)
                return Result.Ok();

            var cacheTags = (await GetCacheTagsAsync(cancellationToken).ConfigureAwait(false))
                .ToDictionary(entry => entry.Key, entry => new HashSet<string>(entry.Value));
            var cacheTypes = (await GetCacheTypesAsync(cancellationToken).ConfigureAwait(false))
                .ToDictionary(entry => entry.Key, entry => new HashSet<string?>(entry.Value));
            var response = await _cache.GetStringAsync(CacheKeys.RequestResponseTypesKey, cancellationToken).ConfigureAwait(false);
            var requestResponseTypes = DeserializeStringMap(response);

            foreach (var requestType in requestTypes)
            {
                cacheTypes.Remove(requestType);
                requestResponseTypes.Remove(requestType);

                foreach (var legacyKey in requestResponseTypes.Keys
                    .Where(key => key.StartsWith(requestType + ",", StringComparison.Ordinal))
                    .ToList())
                {
                    requestResponseTypes.Remove(legacyKey);
                }
            }

            foreach (var tag in cacheTags.Keys.ToList())
            {
                cacheTags[tag].RemoveWhere(requestTypes.Contains);
                if (cacheTags[tag].Count == 0)
                    cacheTags.Remove(tag);
            }

            await PersistIndexAsync(CacheKeys.CacheTagsKey, cacheTags, cancellationToken).ConfigureAwait(false);
            await PersistIndexAsync(CacheKeys.CacheTypesKey, cacheTypes, cancellationToken).ConfigureAwait(false);
            await PersistIndexAsync(CacheKeys.RequestResponseTypesKey, requestResponseTypes, cancellationToken).ConfigureAwait(false);

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(CacheKeys.CacheTagsKey, cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(CacheKeys.CacheTypesKey, cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(CacheKeys.RequestResponseTypesKey, cancellationToken).ConfigureAwait(false);

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    private async Task PersistIndexAsync<TMap>(string key, TMap map, CancellationToken cancellationToken)
        where TMap : System.Collections.ICollection
    {
        if (map.Count == 0)
        {
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return;
        }

        await _cache.SetStringAsync(key, JsonConvert.SerializeObject(map), cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> AddOrUpdateCacheTag<TRequest>(IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (tags == null)
                return Result.Ok();

            var requestTypeName = typeof(TRequest).FullName ?? typeof(TRequest).Name;
            var response = await _cache.GetStringAsync(CacheKeys.CacheTagsKey, cancellationToken).ConfigureAwait(false);
            var cacheTags = response == null
                ? new Dictionary<string, HashSet<string>>()
                : (Dictionary<string, HashSet<string>>)JsonConvert.DeserializeObject(response, typeof(Dictionary<string, HashSet<string>>))!;

            var changed = false;
            foreach (var tag in tags)
            {
                if (!cacheTags.TryGetValue(tag, out HashSet<string>? tagTypes) || tagTypes is null)
                {
                    tagTypes = [];
                    cacheTags[tag] = tagTypes;
                    changed = true;
                }

                if (tagTypes.Add(requestTypeName))
                    changed = true;
            }

            if (changed)
            {
                await _cache.SetStringAsync(
                    CacheKeys.CacheTagsKey,
                    JsonConvert.SerializeObject(cacheTags),
                    cancellationToken).ConfigureAwait(false);
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

            var requestTypeName = typeof(TRequest).FullName ?? typeof(TRequest).Name;
            var response = await _cache.GetStringAsync(CacheKeys.CacheTypesKey, cancellationToken).ConfigureAwait(false);
            var cacheTypes = response == null
                ? new Dictionary<string, HashSet<string?>>()
                : (Dictionary<string, HashSet<string?>>)JsonConvert.DeserializeObject(response, typeof(Dictionary<string, HashSet<string?>>))!;

            var changed = false;
            if (!cacheTypes.TryGetValue(requestTypeName, out HashSet<string?>? types) || types is null)
            {
                types = [];
                cacheTypes[requestTypeName] = types;
                changed = true;
            }

            if (types.Add(cacheKey))
                changed = true;

            if (changed)
            {
                await _cache.SetStringAsync(
                    CacheKeys.CacheTypesKey,
                    JsonConvert.SerializeObject(cacheTypes),
                    cancellationToken).ConfigureAwait(false);
            }

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    private static Dictionary<string, string> DeserializeStringMap(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, string>();

        return JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Resolves a response type name using the current FullName key, or a legacy
    /// AssemblyQualifiedName key written by older Dictionary&lt;Type, Type&gt; serialization.
    /// </summary>
    private static bool TryResolveResponseTypeName(
        Dictionary<string, string> map,
        string requestTypeName,
        out string? responseTypeName)
    {
        if (map.TryGetValue(requestTypeName, out responseTypeName)
            && !string.IsNullOrEmpty(responseTypeName))
        {
            return true;
        }

        foreach (var entry in map)
        {
            if (entry.Key.StartsWith(requestTypeName + ",", StringComparison.Ordinal)
                && !string.IsNullOrEmpty(entry.Value))
            {
                responseTypeName = entry.Value;
                return true;
            }
        }

        responseTypeName = null;
        return false;
    }
}
