using FluentResults;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Garnet.Constants;
using StackExchange.Redis;
using System.Collections.ObjectModel;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Containers;

public class GarnetOutputCacheContainer : IRequestOutputCacheContainer
{
    private const int MaxIndexUpdateAttempts = 8;

    private readonly IContainerIndexStore _index;

    /// <summary>
    /// Initializes a new instance that updates the container indexes through the distributed cache.
    /// Concurrent writers sharing one instance can overwrite each other's index entries; prefer the
    /// overload taking an <see cref="IConnectionMultiplexer"/> for multi-replica deployments.
    /// </summary>
    /// <param name="cache">The distributed cache holding the container indexes.</param>
    public GarnetOutputCacheContainer(IDistributedCache cache)
        : this(new DistributedCacheIndexStore(cache))
    {
    }

    /// <summary>
    /// Initializes a new instance that merges the container indexes atomically, so replicas sharing
    /// one Garnet instance keep each other's index entries.
    /// </summary>
    /// <param name="cache">The distributed cache holding the container indexes.</param>
    /// <param name="connectionMultiplexer">The connection used for compare-and-swap index writes.</param>
    /// <param name="cacheOptions">The cache options providing the configured key prefix.</param>
    public GarnetOutputCacheContainer(
        IDistributedCache cache,
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisCacheOptions> cacheOptions)
        : this(CreateAtomicIndexStore(cache, connectionMultiplexer, cacheOptions))
    {
    }

    internal GarnetOutputCacheContainer(IContainerIndexStore index)
    {
        _index = index;
    }

    public async Task<Type?> GetResponseTypeAsync<TRequest>(CancellationToken cancellationToken = default)
    {
        var response = await _index.ReadAsync(CacheKeys.RequestResponseTypesKey, cancellationToken).ConfigureAwait(false);
        if (response == null) return null;

        var requestResponseTypes = DeserializeStringMap(response);
        var requestTypeName = GetRequestTypeName<TRequest>();

        if (!TryResolveResponseTypeName(requestResponseTypes, requestTypeName, out var responseTypeName)
            || string.IsNullOrEmpty(responseTypeName))
        {
            return null;
        }

        return Type.GetType(responseTypeName);
    }

    public async Task<Result> UpdateContainerAsync<TRequest>(IEnumerable<string>? tags = null, string? cacheKey = null, Type? responseType = null, CancellationToken cancellationToken = default)
    {
        var updateCacheTag = await AddOrUpdateCacheTag<TRequest>(tags, cancellationToken).ConfigureAwait(false);
        var updateCacheType = await AddOrUpdateCacheType<TRequest>(cacheKey, cancellationToken).ConfigureAwait(false);

        if (!updateCacheTag.IsSuccess || !updateCacheType.IsSuccess)
            return Result.Fail(ErrorMessages.ContainerUpdatesFails);

        if (responseType is null)
            return Result.Ok();

        return await AddOrUpdateResponseType<TRequest>(responseType, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReadOnlyDictionary<string, HashSet<string>>> GetCacheTagsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _index.ReadAsync(CacheKeys.CacheTagsKey, cancellationToken).ConfigureAwait(false);
        if (response == null) return new Dictionary<string, HashSet<string>>().AsReadOnly();

        return DeserializeTagMap(response).AsReadOnly();
    }

    public async Task<ReadOnlyDictionary<string, HashSet<string?>>> GetCacheTypesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _index.ReadAsync(CacheKeys.CacheTypesKey, cancellationToken).ConfigureAwait(false);
        if (response == null) return new Dictionary<string, HashSet<string?>>().AsReadOnly();

        return DeserializeTypeMap(response).AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveRequestTypesAsync(
        IEnumerable<string> requestTypeNames,
        CancellationToken cancellationToken = default)
    {
        var requestTypes = requestTypeNames.ToHashSet(StringComparer.Ordinal);
        if (requestTypes.Count == 0)
            return Result.Ok();

        var tagsResult = await MutateIndexAsync(
            CacheKeys.CacheTagsKey,
            current => RemoveFromTagMap(current, requestTypes),
            cancellationToken).ConfigureAwait(false);
        if (tagsResult.IsFailed)
            return tagsResult;

        var typesResult = await MutateIndexAsync(
            CacheKeys.CacheTypesKey,
            current => RemoveFromTypeMap(current, requestTypes),
            cancellationToken).ConfigureAwait(false);
        if (typesResult.IsFailed)
            return typesResult;

        return await MutateIndexAsync(
            CacheKeys.RequestResponseTypesKey,
            current => RemoveFromResponseTypeMap(current, requestTypes),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _index.RemoveAsync(CacheKeys.CacheTagsKey, cancellationToken).ConfigureAwait(false);
            await _index.RemoveAsync(CacheKeys.CacheTypesKey, cancellationToken).ConfigureAwait(false);
            await _index.RemoveAsync(CacheKeys.RequestResponseTypesKey, cancellationToken).ConfigureAwait(false);

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    private static IContainerIndexStore CreateAtomicIndexStore(
        IDistributedCache cache,
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisCacheOptions> cacheOptions)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(cacheOptions);

        return new GarnetIndexStore(
            connectionMultiplexer,
            cacheOptions.Value.InstanceName,
            new DistributedCacheIndexStore(cache));
    }

    /// <summary>
    /// Reads an index document, applies <paramref name="merge"/> and writes it back, retrying while
    /// concurrent writers keep replacing the document.
    /// </summary>
    private async Task<Result> MutateIndexAsync(
        string key,
        Func<string?, ContainerIndexUpdate> merge,
        CancellationToken cancellationToken)
    {
        try
        {
            for (var attempt = 0; attempt < MaxIndexUpdateAttempts; attempt++)
            {
                var current = await _index.ReadAsync(key, cancellationToken).ConfigureAwait(false);

                var update = merge(current);
                if (!update.Changed)
                    return Result.Ok();

                if (await _index.TryUpdateAsync(key, current, update.Value, cancellationToken).ConfigureAwait(false))
                    return Result.Ok();
            }

            return Result.Fail(ErrorMessages.ContainerUpdatesFails);
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    private Task<Result> AddOrUpdateCacheTag<TRequest>(IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
    {
        if (tags == null)
            return Task.FromResult(Result.Ok());

        var tagNames = tags.ToArray();
        var requestTypeName = GetRequestTypeName<TRequest>();

        return MutateIndexAsync(CacheKeys.CacheTagsKey, current =>
        {
            var cacheTags = DeserializeTagMap(current);

            var changed = false;
            foreach (var tag in tagNames)
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

            return changed
                ? ContainerIndexUpdate.Write(JsonConvert.SerializeObject(cacheTags))
                : ContainerIndexUpdate.Unchanged;
        }, cancellationToken);
    }

    private Task<Result> AddOrUpdateCacheType<TRequest>(string? cacheKey = null, CancellationToken cancellationToken = default)
    {
        if (cacheKey == null)
            return Task.FromResult(Result.Ok());

        var requestTypeName = GetRequestTypeName<TRequest>();

        return MutateIndexAsync(CacheKeys.CacheTypesKey, current =>
        {
            var cacheTypes = DeserializeTypeMap(current);

            var changed = false;
            if (!cacheTypes.TryGetValue(requestTypeName, out HashSet<string?>? types) || types is null)
            {
                types = [];
                cacheTypes[requestTypeName] = types;
                changed = true;
            }

            if (types.Add(cacheKey))
                changed = true;

            return changed
                ? ContainerIndexUpdate.Write(JsonConvert.SerializeObject(cacheTypes))
                : ContainerIndexUpdate.Unchanged;
        }, cancellationToken);
    }

    private Task<Result> AddOrUpdateResponseType<TRequest>(Type responseType, CancellationToken cancellationToken)
    {
        var requestTypeName = GetRequestTypeName<TRequest>();
        var responseTypeName = responseType.AssemblyQualifiedName ?? responseType.FullName ?? responseType.Name;

        return MutateIndexAsync(CacheKeys.RequestResponseTypesKey, current =>
        {
            var requestResponseTypes = DeserializeStringMap(current);

            if (requestResponseTypes.TryGetValue(requestTypeName, out var existing)
                && string.Equals(existing, responseTypeName, StringComparison.Ordinal))
            {
                return ContainerIndexUpdate.Unchanged;
            }

            requestResponseTypes[requestTypeName] = responseTypeName;
            return ContainerIndexUpdate.Write(JsonConvert.SerializeObject(requestResponseTypes));
        }, cancellationToken);
    }

    private static ContainerIndexUpdate RemoveFromTagMap(string? current, HashSet<string> requestTypes)
    {
        var cacheTags = DeserializeTagMap(current);

        var changed = false;
        foreach (var tag in cacheTags.Keys.ToList())
        {
            var tagTypes = cacheTags[tag];
            if (tagTypes is not null && tagTypes.RemoveWhere(requestTypes.Contains) > 0)
                changed = true;

            if (tagTypes is null || tagTypes.Count == 0)
            {
                cacheTags.Remove(tag);
                changed = true;
            }
        }

        return Materialize(changed, cacheTags);
    }

    private static ContainerIndexUpdate RemoveFromTypeMap(string? current, HashSet<string> requestTypes)
    {
        var cacheTypes = DeserializeTypeMap(current);

        var changed = false;
        foreach (var requestType in requestTypes)
        {
            if (cacheTypes.Remove(requestType))
                changed = true;
        }

        return Materialize(changed, cacheTypes);
    }

    private static ContainerIndexUpdate RemoveFromResponseTypeMap(string? current, HashSet<string> requestTypes)
    {
        var requestResponseTypes = DeserializeStringMap(current);

        var changed = false;
        foreach (var requestType in requestTypes)
        {
            if (requestResponseTypes.Remove(requestType))
                changed = true;

            foreach (var legacyKey in requestResponseTypes.Keys
                .Where(key => key.StartsWith(requestType + ",", StringComparison.Ordinal))
                .ToList())
            {
                requestResponseTypes.Remove(legacyKey);
                changed = true;
            }
        }

        return Materialize(changed, requestResponseTypes);
    }

    private static ContainerIndexUpdate Materialize<TMap>(bool changed, TMap map)
        where TMap : System.Collections.ICollection
    {
        if (!changed)
            return ContainerIndexUpdate.Unchanged;

        return map.Count == 0
            ? ContainerIndexUpdate.Delete
            : ContainerIndexUpdate.Write(JsonConvert.SerializeObject(map));
    }

    private static string GetRequestTypeName<TRequest>() =>
        typeof(TRequest).FullName ?? typeof(TRequest).Name;

    private static Dictionary<string, HashSet<string>> DeserializeTagMap(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, HashSet<string>>();

        return JsonConvert.DeserializeObject<Dictionary<string, HashSet<string>>>(json)
               ?? new Dictionary<string, HashSet<string>>();
    }

    private static Dictionary<string, HashSet<string?>> DeserializeTypeMap(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, HashSet<string?>>();

        return JsonConvert.DeserializeObject<Dictionary<string, HashSet<string?>>>(json)
               ?? new Dictionary<string, HashSet<string?>>();
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
