using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Helpers;
using NexGen.MediatR.Extensions.Caching.Redis;
using NexGen.MediatR.Extensions.Caching.Redis.Containers;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Redis;

public sealed class RedisOutputCacheContainerTests
{
    private static readonly string RequestResponseTypesKey =
        RequestCacheConstants.CacheKeyRootPrefix + ":Container:RequestResponseTypes";

    private static readonly string CacheTypesKey =
        RequestCacheConstants.CacheKeyRootPrefix + ":Container:CacheTypes";

    private sealed record LocalQuery(int Id) : IRequest<string>;

    [Fact]
    public async Task UpdateContainer_WithForeignTypeEntries_DoesNotFail()
    {
        var store = new InMemoryDistributedCache();
        var foreignMap = new Dictionary<string, string>
        {
            ["OtherService.Queries.ForeignQuery, OtherService, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"] =
                "OtherService.Dtos.ForeignDto, OtherService, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        };

        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(foreignMap));

        var container = new RedisOutputCacheContainer(store);
        var result = await container.UpdateContainerAsync<LocalQuery>(
            tags: ["User"],
            cacheKey: "key-1",
            responseType: typeof(string));

        Assert.True(result.IsSuccess);

        var stored = await store.GetStringAsync(RequestResponseTypesKey);
        Assert.NotNull(stored);
        var map = JsonConvert.DeserializeObject<Dictionary<string, string>>(stored!)!;
        Assert.Contains(foreignMap.Keys.First(), map.Keys);
        Assert.Contains(typeof(LocalQuery).FullName!, map.Keys);
    }

    [Fact]
    public async Task SetAsync_WithForeignTypeEntries_StillWritesResponse()
    {
        var store = new InMemoryDistributedCache();
        var foreignMap = new Dictionary<string, string>
        {
            ["OtherService.Queries.ForeignQuery, OtherService"] = "OtherService.Dtos.ForeignDto, OtherService"
        };
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(foreignMap));

        // Legacy Dictionary<Type,Type> JSON would throw on deserialize in the old code path.
        // Also seed a legacy-looking blob that would break Type-keyed deserialization:
        // (string map above is enough; additionally verify SetAsync succeeds end-to-end)

        var cache = new RedisRequestOutputCache<LocalQuery, string>(
            NullLogger<RedisRequestOutputCache<LocalQuery, string>>.Instance,
            store,
            new RedisOutputCacheContainer(store));

        var query = new LocalQuery(7);
        var set = await cache.SetAsync(query, "payload", tags: ["User"], expirationInSeconds: 60);
        Assert.True(set.IsSuccess);

        var cacheKey = RequestOutputCacheHelper.GetCacheKey(query);
        var response = await store.GetStringAsync(cacheKey);
        Assert.Equal(JsonConvert.SerializeObject("payload"), response);

        var hit = await cache.GetAsync(query);
        Assert.True(hit.IsSuccess);
        Assert.Equal("payload", hit.Value);
    }

    [Fact]
    public async Task UpdateContainer_PersistsSecondCacheKey_ForExistingRequestType()
    {
        var store = new InMemoryDistributedCache();
        var container = new RedisOutputCacheContainer(store);

        var first = await container.UpdateContainerAsync<LocalQuery>(
            tags: ["User"],
            cacheKey: "key-first",
            responseType: typeof(string));
        Assert.True(first.IsSuccess);

        var second = await container.UpdateContainerAsync<LocalQuery>(
            tags: ["User"],
            cacheKey: "key-second",
            responseType: typeof(string));
        Assert.True(second.IsSuccess);

        var typesJson = await store.GetStringAsync(CacheTypesKey);
        Assert.NotNull(typesJson);
        var cacheTypes = JsonConvert.DeserializeObject<Dictionary<string, HashSet<string?>>>(typesJson!)!;
        var requestTypeName = typeof(LocalQuery).FullName!;
        Assert.True(cacheTypes.TryGetValue(requestTypeName, out var keys));
        Assert.Contains("key-first", keys!);
        Assert.Contains("key-second", keys!);
    }

    private sealed class InMemoryDistributedCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, byte[]> _store = new();

        public byte[]? Get(string key) =>
            _store.TryGetValue(key, out var value) ? value : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromResult(Get(key));

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            Task.CompletedTask;

        public void Remove(string key) => _store.TryRemove(key, out _);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            _store[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}
