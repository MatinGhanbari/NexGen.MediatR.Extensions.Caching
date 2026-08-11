using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Garnet;
using NexGen.MediatR.Extensions.Caching.Garnet.Containers;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Garnet;

/// <summary>
/// Garnet mirrors Redis container serialization; keep critical multi-app paths covered here.
/// </summary>
public sealed class GarnetOutputCacheContainerTests
{
    private static readonly string RequestResponseTypesKey =
        RequestCacheConstants.CacheKeyRootPrefix + ":Container:RequestResponseTypes";

    private static readonly string CacheTypesKey =
        RequestCacheConstants.CacheKeyRootPrefix + ":Container:CacheTypes";

    private sealed record LocalQuery(int Id) : IRequest<string>;
    private sealed record AppAQuery(int Id) : IRequest<string>;
    private sealed record AppBQuery(string Name) : IRequest<string>;

    [Fact]
    public async Task SetAsync_WithForeignTypeEntries_StillWritesResponse()
    {
        var store = new InMemoryDistributedCache();
        var foreignMap = new Dictionary<string, string>
        {
            ["OtherService.Queries.ForeignQuery, OtherService"] = "OtherService.Dtos.ForeignDto, OtherService"
        };
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(foreignMap));

        var cache = new GarnetRequestOutputCache<LocalQuery, string>(
            NullLogger<GarnetRequestOutputCache<LocalQuery, string>>.Instance,
            store,
            new GarnetOutputCacheContainer(store));

        var query = new LocalQuery(7);
        var set = await cache.SetAsync(query, "payload", tags: ["User"], expirationInSeconds: 60);
        Assert.True(set.IsSuccess);

        var cacheKey = RequestOutputCacheHelper.GetCacheKey(query);
        Assert.Equal(JsonConvert.SerializeObject("payload"), await store.GetStringAsync(cacheKey));

        var hit = await cache.GetAsync(query);
        Assert.True(hit.IsSuccess);
        Assert.Equal("payload", hit.Value);
    }

    [Fact]
    public async Task UpdateContainer_PersistsSecondCacheKey_ForExistingRequestType()
    {
        var store = new InMemoryDistributedCache();
        var container = new GarnetOutputCacheContainer(store);

        Assert.True((await container.UpdateContainerAsync<LocalQuery>(
            tags: ["User"], cacheKey: "key-first", responseType: typeof(string))).IsSuccess);
        Assert.True((await container.UpdateContainerAsync<LocalQuery>(
            tags: ["User"], cacheKey: "key-second", responseType: typeof(string))).IsSuccess);

        var cacheTypes = JsonConvert.DeserializeObject<Dictionary<string, HashSet<string?>>>(
            (await store.GetStringAsync(CacheTypesKey))!)!;
        var keys = cacheTypes[typeof(LocalQuery).FullName!];
        Assert.Contains("key-first", keys);
        Assert.Contains("key-second", keys);
    }

    [Fact]
    public async Task TwoApps_SameStore_BothCanSetAndGet()
    {
        var shared = new InMemoryDistributedCache();
        var appA = CreateCache<AppAQuery>(shared);
        var appB = CreateCache<AppBQuery>(shared);

        var aQuery = new AppAQuery(1);
        var bQuery = new AppBQuery("x");
        Assert.True((await appA.SetAsync(aQuery, "a-payload", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(bQuery, "b-payload", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);

        Assert.Equal("a-payload", (await appA.GetAsync(aQuery)).Value);
        Assert.Equal("b-payload", (await appB.GetAsync(bQuery)).Value);
    }

    [Fact]
    public async Task GetResponseType_ResolvesLegacyAssemblyQualifiedNameKey()
    {
        var store = new InMemoryDistributedCache();
        var legacy = new Dictionary<string, string>
        {
            [typeof(LocalQuery).AssemblyQualifiedName!] = typeof(string).AssemblyQualifiedName!
        };
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(legacy));

        var resolved = await new GarnetOutputCacheContainer(store).GetResponseTypeAsync<LocalQuery>();
        Assert.Equal(typeof(string), resolved);
    }

    private static GarnetRequestOutputCache<TRequest, string> CreateCache<TRequest>(IDistributedCache store)
        where TRequest : IRequest<string> =>
        new(
            NullLogger<GarnetRequestOutputCache<TRequest, string>>.Instance,
            store,
            new GarnetOutputCacheContainer(store));

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
