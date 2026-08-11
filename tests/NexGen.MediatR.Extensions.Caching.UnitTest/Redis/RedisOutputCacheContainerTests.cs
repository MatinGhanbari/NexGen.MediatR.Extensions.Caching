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

    private sealed record AppAQuery(int Id) : IRequest<string>;
    private sealed record AppBQuery(string Name) : IRequest<string>;
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

        var cache = CreateCache<LocalQuery>(store);

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

    [Fact]
    public async Task TwoApps_SameRedis_NoInstanceName_BothCanSetAndGet()
    {
        var shared = new InMemoryDistributedCache();
        var appA = CreateCache<AppAQuery>(shared);
        var appB = CreateCache<AppBQuery>(shared);

        var aQuery = new AppAQuery(1);
        var bQuery = new AppBQuery("x");

        Assert.True((await appA.SetAsync(aQuery, "a-payload", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(bQuery, "b-payload", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);

        var aHit = await appA.GetAsync(aQuery);
        var bHit = await appB.GetAsync(bQuery);
        Assert.True(aHit.IsSuccess);
        Assert.True(bHit.IsSuccess);
        Assert.Equal("a-payload", aHit.Value);
        Assert.Equal("b-payload", bHit.Value);

        var typeMapJson = await shared.GetStringAsync(RequestResponseTypesKey);
        var typeMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(typeMapJson!)!;
        Assert.Contains(typeof(AppAQuery).FullName!, typeMap.Keys);
        Assert.Contains(typeof(AppBQuery).FullName!, typeMap.Keys);
    }

    [Fact]
    public async Task TwoApps_DifferentInstanceName_ContainersAreIsolated()
    {
        var shared = new InMemoryDistributedCache();
        var appAStore = new PrefixedDistributedCache(shared, "app-a:");
        var appBStore = new PrefixedDistributedCache(shared, "app-b:");

        var appA = CreateCache<AppAQuery>(appAStore);
        var appB = CreateCache<AppBQuery>(appBStore);

        Assert.True((await appA.SetAsync(new AppAQuery(1), "a", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(new AppBQuery("x"), "b", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        Assert.NotNull(await shared.GetStringAsync("app-a:" + RequestResponseTypesKey));
        Assert.NotNull(await shared.GetStringAsync("app-b:" + RequestResponseTypesKey));
        Assert.Null(await shared.GetStringAsync(RequestResponseTypesKey));

        var aMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            (await shared.GetStringAsync("app-a:" + RequestResponseTypesKey))!)!;
        var bMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            (await shared.GetStringAsync("app-b:" + RequestResponseTypesKey))!)!;

        Assert.Contains(typeof(AppAQuery).FullName!, aMap.Keys);
        Assert.DoesNotContain(typeof(AppBQuery).FullName!, aMap.Keys);
        Assert.Contains(typeof(AppBQuery).FullName!, bMap.Keys);
        Assert.DoesNotContain(typeof(AppAQuery).FullName!, bMap.Keys);
    }

    [Fact]
    public async Task SharedTag_WithoutInstanceName_EvictAffectsBothApps()
    {
        var shared = new InMemoryDistributedCache();
        var appA = CreateCache<AppAQuery>(shared);
        var appB = CreateCache<AppBQuery>(shared);

        var aQuery = new AppAQuery(1);
        var bQuery = new AppBQuery("x");
        Assert.True((await appA.SetAsync(aQuery, "a", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(bQuery, "b", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        Assert.True((await appA.EvictByTagsAsync(["User"])).IsSuccess);

        Assert.Null(await shared.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(aQuery)));
        Assert.Null(await shared.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(bQuery)));
    }

    [Fact]
    public async Task SharedTag_WithInstanceName_EvictIsIsolated()
    {
        var shared = new InMemoryDistributedCache();
        var appAStore = new PrefixedDistributedCache(shared, "app-a:");
        var appBStore = new PrefixedDistributedCache(shared, "app-b:");
        var appA = CreateCache<AppAQuery>(appAStore);
        var appB = CreateCache<AppBQuery>(appBStore);

        var aQuery = new AppAQuery(1);
        var bQuery = new AppBQuery("x");
        Assert.True((await appA.SetAsync(aQuery, "a", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(bQuery, "b", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        Assert.True((await appA.EvictByTagsAsync(["User"])).IsSuccess);

        Assert.Null(await appAStore.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(aQuery)));
        Assert.Equal(
            JsonConvert.SerializeObject("b"),
            await appBStore.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(bQuery)));
    }

    [Fact]
    public async Task FlushAll_WithoutInstanceName_RemovesOtherAppEntries()
    {
        var shared = new InMemoryDistributedCache();
        var appA = CreateCache<AppAQuery>(shared);
        var appB = CreateCache<AppBQuery>(shared);

        var aQuery = new AppAQuery(1);
        var bQuery = new AppBQuery("x");
        Assert.True((await appA.SetAsync(aQuery, "a", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(bQuery, "b", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);

        Assert.True((await appA.FlushAll()).IsSuccess);

        Assert.Null(await shared.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(aQuery)));
        Assert.Null(await shared.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(bQuery)));
    }

    [Fact]
    public async Task Legacy_DictionaryTypeType_Json_DoesNotBlockSetAsync()
    {
        var store = new InMemoryDistributedCache();

        // Mimic pre-1.4.2 serialization of Dictionary<Type, Type> (keys/values are AQN strings in JSON).
        var legacyMap = new Dictionary<Type, Type>
        {
            [typeof(string)] = typeof(int)
        };
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(legacyMap));

        // Plus an unresolved foreign assembly entry (string form as left in Redis).
        var seeded = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            (await store.GetStringAsync(RequestResponseTypesKey))!)!;
        seeded["OtherService.Queries.ForeignQuery, OtherService, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"] =
            "OtherService.Dtos.ForeignDto, OtherService, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(seeded));

        var cache = CreateCache<LocalQuery>(store);
        var query = new LocalQuery(3);
        var set = await cache.SetAsync(query, "ok", tags: ["User"], expirationInSeconds: 30);
        Assert.True(set.IsSuccess);
        Assert.Equal(JsonConvert.SerializeObject("ok"), await store.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(query)));
    }

    [Fact]
    public async Task GetResponseType_ResolvesLegacyAssemblyQualifiedNameKey()
    {
        var store = new InMemoryDistributedCache();
        var requestAqn = typeof(LocalQuery).AssemblyQualifiedName!;
        var responseAqn = typeof(string).AssemblyQualifiedName!;
        var legacy = new Dictionary<string, string> { [requestAqn] = responseAqn };
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(legacy));

        var container = new RedisOutputCacheContainer(store);
        var resolved = await container.GetResponseTypeAsync<LocalQuery>();
        Assert.Equal(typeof(string), resolved);
    }

    [Fact]
    public async Task GetAsync_WithForeignEntries_StillHits()
    {
        var store = new InMemoryDistributedCache();
        var cache = CreateCache<LocalQuery>(store);
        var query = new LocalQuery(9);

        Assert.True((await cache.SetAsync(query, "hit-me", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        var map = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            (await store.GetStringAsync(RequestResponseTypesKey))!)!;
        map["Foreign.App.Query, Foreign"] = "Foreign.App.Dto, Foreign";
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(map));

        var hit = await cache.GetAsync(query);
        Assert.True(hit.IsSuccess);
        Assert.Equal("hit-me", hit.Value);
    }

    [Fact]
    public async Task Concurrent_UpdateContainer_LastWriteWins_CanDropPeerMetadata()
    {
        var inner = new InMemoryDistributedCache();
        var coordinated = new CoordinatedReadDistributedCache(inner, RequestResponseTypesKey, readersBeforeRelease: 2);
        var containerA = new RedisOutputCacheContainer(coordinated);
        var containerB = new RedisOutputCacheContainer(coordinated);

        await Task.WhenAll(
            containerA.UpdateContainerAsync<AppAQuery>(tags: ["User"], cacheKey: "a-key", responseType: typeof(string)),
            containerB.UpdateContainerAsync<AppBQuery>(tags: ["Order"], cacheKey: "b-key", responseType: typeof(string)));

        var typeMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            (await inner.GetStringAsync(RequestResponseTypesKey))!)!;

        var aPresent = typeMap.ContainsKey(typeof(AppAQuery).FullName!);
        var bPresent = typeMap.ContainsKey(typeof(AppBQuery).FullName!);

        // Documented production risk: without compare-and-swap, concurrent writers can drop peer entries.
        Assert.False(aPresent && bPresent,
            "Expected last-write-wins to drop one peer's response-type entry under coordinated concurrent reads.");
        Assert.True(aPresent || bPresent);
    }

    private static RedisRequestOutputCache<TRequest, string> CreateCache<TRequest>(IDistributedCache store)
        where TRequest : IRequest<string> =>
        new(
            NullLogger<RedisRequestOutputCache<TRequest, string>>.Instance,
            store,
            new RedisOutputCacheContainer(store));

    private sealed class PrefixedDistributedCache : IDistributedCache
    {
        private readonly IDistributedCache _inner;
        private readonly string _prefix;

        public PrefixedDistributedCache(IDistributedCache inner, string prefix)
        {
            _inner = inner;
            _prefix = prefix;
        }

        private string Key(string key) => _prefix + key;

        public byte[]? Get(string key) => _inner.Get(Key(key));

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            _inner.GetAsync(Key(key), token);

        public void Refresh(string key) => _inner.Refresh(Key(key));

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            _inner.RefreshAsync(Key(key), token);

        public void Remove(string key) => _inner.Remove(Key(key));

        public Task RemoveAsync(string key, CancellationToken token = default) =>
            _inner.RemoveAsync(Key(key), token);

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            _inner.Set(Key(key), value, options);

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
            _inner.SetAsync(Key(key), value, options, token);
    }

    /// <summary>
    /// Holds GetAsync on a specific key until N readers have arrived, forcing a last-write-wins race.
    /// </summary>
    private sealed class CoordinatedReadDistributedCache : IDistributedCache
    {
        private readonly InMemoryDistributedCache _inner;
        private readonly string _coordinateKey;
        private readonly int _readersBeforeRelease;
        private int _readersWaiting;
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CoordinatedReadDistributedCache(
            InMemoryDistributedCache inner,
            string coordinateKey,
            int readersBeforeRelease)
        {
            _inner = inner;
            _coordinateKey = coordinateKey;
            _readersBeforeRelease = readersBeforeRelease;
        }

        public byte[]? Get(string key) => _inner.Get(key);

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            if (!string.Equals(key, _coordinateKey, StringComparison.Ordinal))
                return await _inner.GetAsync(key, token).ConfigureAwait(false);

            // Snapshot before release so both callers merge from the same pre-write view.
            var snapshot = await _inner.GetAsync(key, token).ConfigureAwait(false);

            if (Interlocked.Increment(ref _readersWaiting) >= _readersBeforeRelease)
                _release.TrySetResult();

            await _release.Task.WaitAsync(token).ConfigureAwait(false);
            return snapshot;
        }

        public void Refresh(string key) => _inner.Refresh(key);

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            _inner.RefreshAsync(key, token);

        public void Remove(string key) => _inner.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default) =>
            _inner.RemoveAsync(key, token);

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            _inner.Set(key, value, options);

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
            _inner.SetAsync(key, value, options, token);
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
